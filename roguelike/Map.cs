using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace roguelike
{
    [Serializable()]
    public struct Tile
    {
        private bool cW;
        private bool explored;
        public char prop;
        private bool outside;

        public bool canWalk { get { return cW; } set { cW = value; } }
        public bool isExplored { get { return explored; } set { explored = value; } }
        public bool isOutside { get { return outside; } set { outside = value; } }
    }

    [Serializable()]
    public struct Level
    {
        private bool random;
        private Tile[] layout;
        public int startx, starty, endx, endy;
        public List<ActorStore> actors;

        public bool isRandom { get { return random; } set { random = value; } }
        public Tile[] theLayout { get { return layout; } set { layout = value; } }

        public Level(bool random, Tile[] layout)
        {
            this.random = random;
            this.layout = layout;
            this.startx = 0;
            this.starty = 0;
            this.endx = 0;
            this.endy = 0;
            this.actors = null;
        }
    }

    public class Room
    {
        public int x1, y1, x2, y2, centerx, centery;
        public Room(int x, int y, int w, int h)
        {
            this.x1 = x;
            this.x2 = x + w;
            this.y1 = y;
            this.y2 = y + h;
            centerx = (x1 + x2) / 2;
            centery = (y1 + y2) / 2;
        }

        public bool intersect(Room other)
        {
            return (x1 <= other.x2 && x2 >= other.x1 && y1 <= other.y2 && y2 >= other.y1);
        }
    }

    public class Map
    {
        int width, height;
        int lastx, lasty;
        public int entx, enty, exx, exy;
        Engine engine;
        TCODMap tmap;
        public Tile[] tiles;
        List<Room> rooms;
        State gameState;

        public Map(int width, int height, Engine engine)
        {
            this.tiles = new Tile[width * height];
            this.tmap = new TCODMap(width, height);
            this.gameState = engine.gameState;

            this.rooms = new List<Room>();

            this.width = width;
            this.height = height;
            this.engine = engine;

            reGenMap(gameState.levellist[gameState.curLevel].theLayout, false, true);
        }

        public void changeLevel(bool forward)
        {
            engine.gStatus = Engine.Status.LVLCHG;
            Tile[] nextLvl = saveAndLoad(forward);
            engine.cleanUp();
            cleanUp();
            loadMap(nextLvl, forward);
        }

        public Tile[] saveAndLoad(bool forward)
        {
            Tile[] prevLvl = new Tile[width*height];
            tiles.CopyTo(prevLvl, 0);
            return gameState.changeLvl(forward, prevLvl, engine, entx, enty, exx, exy);
        }

        public void cleanUp()
        {
            tmap.clear(false, false);
            rooms.Clear();
            rooms.TrimExcess();

            tiles = new Tile[width * height];
        }

        public void loadMap(Tile[] nextLvl, bool forward)
        {
            engine.gui.loadMapImg();
            if (nextLvl == null)
            {
                genMap();
                return;
            }
            reGenMap(nextLvl, forward);
            return;
        }

        public void genMap()
        {
            int numRooms = 0;
            int newx, newy, prevx, prevy;
            bool failed = false;

            for (var i = 0; i < Globals.ROOMS; i++)
            {
                failed = false;
                int w = TCODRandom.getInstance().getInt(Globals.MIN_SIZE, Globals.MAX_SIZE);
                int h = TCODRandom.getInstance().getInt(Globals.MIN_SIZE, Globals.MAX_SIZE);
                int x = TCODRandom.getInstance().getInt(0, width - w - 1);
                int y = TCODRandom.getInstance().getInt(0, height - h - 1);

                Room newRoom = new Room(x, y, w, h);
                foreach (Room otherroom in rooms)
                {
                    if (newRoom.intersect(otherroom))
                    {
                        failed = true;
                        break;
                    }
                }

                if (!failed)
                {
                    createRoom(newRoom);
                    newx = newRoom.centerx;
                    newy = newRoom.centery;

                    if (numRooms == 0)
                    {
                        engine.player.x = newx;
                        engine.player.y = newy;
                    }
                    else
                    {
                        prevx = rooms[numRooms - 1].centerx;
                        prevy = rooms[numRooms - 1].centery;

                        if (TCODRandom.getInstance().getInt(0, 1) == 1)
                        {
                            hTunnel(prevx, newx, prevy);
                            vTunnel(prevy, newy, newx);
                        }
                        else
                        {
                            vTunnel(prevy, newy, prevx);
                            hTunnel(prevx, newx, newy);
                        }
                    }

                    placeThings(newRoom);
                    rooms.Add(newRoom);
                    numRooms++;
                    lastx = newRoom.centerx;
                    lasty = newRoom.centery;
                }
            }

            entx = engine.player.x+1;
            enty = engine.player.y;
            exx = lastx+1;
            exy = lasty;

            Actor nextLevel = new Actor(exx, exy, '>', "stairs", TCODColor.sea);
            nextLevel.portal = new Portal(this, true, false);
            engine.actors.Add(nextLevel);
            
            if(gameState.curLevel != 0)
            {
                Actor prevLevel = new Actor(entx, enty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false, false);
                engine.actors.Add(prevLevel);
            }
        }

        public void reGenMap(Tile[] level, bool forward, bool loaded = false)
        {
            if (gameState.curLevel == 0 || gameState.curLevel == 1)             {
                engine.player.x = gameState.levellist[gameState.curLevel].startx;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true, false);
                engine.actors.Add(nextLevel);
            }
            else if (gameState.curLevel == 4)
            {
                engine.player.x = gameState.levellist[gameState.curLevel].startx;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor theend = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                theend.portal = new Portal(this, false, true);
                engine.actors.Add(theend);
            }
            else if (forward)
            {
                engine.player.x = gameState.levellist[gameState.curLevel].startx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true, false);
                engine.actors.Add(nextLevel);

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false, false);
                engine.actors.Add(prevLevel);
            }
            else if (loaded)
            {
                engine.player.x = gameState.levellist[gameState.curLevel].startx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false, false);
                engine.actors.Add(prevLevel);

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                
                engine.actors.Add(nextLevel);

                if (gameState.curLevel == 4)
                {
                    nextLevel.portal = new Portal(this, false, true);
                }
                else
                {
                    nextLevel.portal = new Portal(this, false, false);
                }
            }
            else
            {
                engine.player.x = gameState.levellist[gameState.curLevel].endx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].endy;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true, false);
                engine.actors.Add(nextLevel);

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false, false);
                engine.actors.Add(prevLevel);
            }

            tiles = level;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x + y * width].canWalk == true)
                    {
                        tmap.setProperties(x, y, true, true);
                    }
                    if (tiles[x + y * width].prop != '0')
                    {
                        renderProp(x, y, tiles[x + y * width].prop);
                    }
                }
            }

            foreach (ActorStore actordata in gameState.levellist[gameState.curLevel].actors)
            {
                if(actordata.name == "thug" || actordata.name == "gangster")
                {
                    // IN ELEGANT -- FIX IF CAN
                    if (actordata.dead == true)
                    {
                        Actor mon = new Actor(actordata.x, actordata.y, '%', "dead body", TCODColor.red);
                        mon.blocks = false;
                        engine.actors.Add(mon);
                    }
                    else
                    {
                        if (actordata.name == "thug")
                        {
                            Actor mon = new Actor(actordata.x, actordata.y, 't', "thug", TCODColor.darkBlue);
                            mon.destruct = new MonsterDestructible(10, 0, "dead thug", engine);
                            mon.attacker = new Attacker(3);
                            mon.ai = new MonsterAI();
                            engine.actors.Add(mon);
                        }
                        else
                        {
                            Actor mon = new Actor(actordata.x, actordata.y, 'g', "gangster", TCODColor.darkCrimson);
                            mon.destruct = new MonsterDestructible(10, 0, "dead gangster", engine);
                            mon.attacker = new Attacker(3);
                            mon.ai = new MonsterAI();
                            engine.actors.Add(mon);
                        }
                    }
                } else if(actordata.name == "bandage") {
                    Actor healthpot = new Actor(actordata.x, actordata.y, '!', "bandage", TCODColor.violet);
                    healthpot.blocks = false;
                    healthpot.pick = new Healer(4);

                    engine.actors.Add(healthpot);
                }
                else if (actordata.name == "rock")
                {
                    Actor confuse = new Actor(actordata.x, actordata.y, '#', "rock", TCODColor.darkBlue);
                    confuse.blocks = false;
                    confuse.pick = new Confuser(10, 10, engine);

                    engine.actors.Add(confuse);
                }
                else if (actordata.name == "ammo"){
                    Actor light = new Actor(actordata.x, actordata.y, '#', "ammo", TCODColor.darkYellow);
                    light.blocks = false;
                    light.pick = new ammo(engine);

                    engine.actors.Add(light);
                }
                else if (actordata.name == "grenade")
                {
                    Actor fire = new Actor(actordata.x, actordata.y, '#', "grenade", TCODColor.darkRed);
                    fire.blocks = false;
                    fire.pick = new grenade(5, 10, engine);

                    engine.actors.Add(fire);
                }
                else if (actordata.name == "girl")
                {
                    Actor girl = new Actor(actordata.x, actordata.y, (char)TCODSpecialCharacter.Female, "girl", TCODColor.red);
                    girl.pick = new Girl(engine);
                    girl.blocks = false;

                    engine.actors.Add(girl);
                }
            }

            return;
        }

        public void renderProp(int x, int y, char prop)
        {
            if (prop == '#')
            {
                Actor theprop = new Actor(x, y, '#', "desk", new TCODColor(102, 66, 37));
                theprop.blocks = true;
                engine.actors.Add(theprop);
            }
            else if (prop == '%')
            {
                Actor theprop = new Actor(x, y, '%', "a body", TCODColor.desaturatedRed);
                theprop.blocks = false;
                engine.actors.Add(theprop);
            }
            else if (prop == '+')
            {
                Actor door = new Actor(x, y, '+', "door", new TCODColor(102, 66, 37));
                door.blocks = false;
                door.pick = new Door();
                tmap.setProperties(x, y, false, true);
                engine.actors.Add(door);
            }
            else if (prop == '!')
            {
                Actor healthpot = new Actor(x, y, '!', "bandage", TCODColor.violet);
                healthpot.blocks = false;
                healthpot.pick = new Healer(4);

                engine.actors.Add(healthpot);
            }
            else if (prop == 'a')
            {
                Actor gun = new Actor(x, y, '#', "ammo", TCODColor.darkYellow);
                gun.blocks = false;
                gun.pick = new ammo(engine);

                engine.actors.Add(gun);
            }
            else if (prop == 'o')
            {
                Actor confuse = new Actor(x, y, '#', "rock", TCODColor.darkBlue);
                confuse.blocks = false;
                confuse.pick = new Confuser(10, 10, engine);

                engine.actors.Add(confuse);
            }
            else if (prop == 'G')
            {
                Actor gren = new Actor(x, y, '#', "grenade", TCODColor.red);
                gren.blocks = false;
                gren.pick = new grenade(5, 10, engine);

                engine.actors.Add(gren);
            }
            else if (Char.IsNumber(prop) || prop == '(' || prop == ')')
            {
                Actor story = new Actor(x, y, ' ', "trigger", new TCODColor(102, 66, 37));
                story.blocks = false;
                story.pick = new Trigger(prop);
                engine.actors.Add(story);
            }
        }

        public bool isWall(int x, int y)
        {
            return !tmap.isWalkable(x, y);
        }

        public bool isExplored(int x, int y)
        {
            return tiles[x + y * width].isExplored;
        }

        public bool isInView(int x, int y)
        {
            if (tmap.isInFov(x, y))
            {
                tiles[x + y * width].isExplored = true;
                return true;
            }
            return false;
        }

        public void computeView()
        {
            tmap.computeFov(engine.player.x, engine.player.y, engine.fovRadius);
        }

        public bool canWalk(int x, int y)
        {
            if (isWall(x, y))
            {
                return false;
            }
            foreach (Actor actor in engine.actors)
            {
                if (actor.blocks && actor.x == x && actor.y == y)
                {
                    return false;
                }
            }
            return true;
        }

        public void render() {
            TCODColor darkWall;
            TCODColor darkGround;
            TCODColor lightWall;
            TCODColor lightGround;
            TCODColor grass = new TCODColor(129, 145, 84);
            TCODColor darkgrass = new TCODColor(85, 96, 56);
            TCODColor grasswall = new TCODColor(47, 53, 31);
            TCODColor darkgrasswall = new TCODColor(49, 56, 32);

            if (engine.gameState.curLevel == 0 || engine.gameState.curLevel == 1 || engine.gameState.curLevel == 4)
            {
                darkWall = new TCODColor(84, 75, 55);
                darkGround = new TCODColor(160, 148, 120);
                lightWall = new TCODColor(84, 64, 18);
                lightGround = new TCODColor(165, 126, 36);
            }
            else
            {
                darkWall = TCODColor.darkerGrey;
                darkGround = TCODColor.darkGrey;
                lightWall = TCODColor.grey;
                lightGround = TCODColor.lighterGrey;
            }

            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {
                    if (tiles[x + y * width].isOutside)
                    {
                        if (isInView(x, y))
                        {
                            TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? grasswall : grass);
                        }
                        else if (isExplored(x, y))
                        {
                            TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? darkgrasswall : darkgrass);
                        }
                    }
                    else
                    {
                        if (isInView(x, y))
                        {
                            TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? lightWall : lightGround);
                        }
                        else if (isExplored(x, y))
                        {
                            TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? darkWall : darkGround);
                        }
                    }
                }
            }
        }

        void placeThings(Room room)
        {
            placeItems(room);
            placeMonsters(room);
        }

        private void placeItems(Room room)
        {
            int numItems = TCODRandom.getInstance().getInt(0, Globals.MAX_ITEMS);
            float random = TCODRandom.getInstance().getFloat(0.0f, 1.0f);

            for (int i = 0; i < numItems; i++)
            {
                random = TCODRandom.getInstance().getFloat(0.0f, 1.0f);
                int x = TCODRandom.getInstance().getInt(room.x1+1, room.x2-1);
                int y = TCODRandom.getInstance().getInt(room.y1+1, room.y2-1);
                
                if (random < .5)
                {
                    Actor healthpot = new Actor(x, y, '!', "bandage", TCODColor.violet);
                    healthpot.blocks = false;
                    healthpot.pick = new Healer(4);

                    engine.actors.Add(healthpot);
                }
                else if (random >= .5 && random < .6)
                {
                    Actor confuse = new Actor(x, y, '#', "rock", TCODColor.darkBlue);
                    confuse.blocks = false;
                    confuse.pick = new Confuser(10, 10, engine);

                    engine.actors.Add(confuse);
                }
                else if (random >= .6 && random < .9){
                    Actor gun = new Actor(x, y, '#', "ammo", TCODColor.darkYellow);
                    gun.blocks = false;
                    gun.pick = new ammo(engine);

                    engine.actors.Add(gun);
                } else
                {
                    Actor fire = new Actor(x, y, '#', "grenade", TCODColor.darkRed);
                    fire.blocks = false;
                    fire.pick = new grenade(5, 10, engine);

                    engine.actors.Add(fire);
                }
            }
        }

        private void placeMonsters(Room room)
        {
            int monsters = TCODRandom.getInstance().getInt(0, Globals.MAX_MON);
            for (var i = 0; i < monsters; i++)
            {
                int mx = TCODRandom.getInstance().getInt(room.x1+1, room.x2-1);
                int my = TCODRandom.getInstance().getInt(room.y1+1, room.y2-1);

                if (TCODRandom.getInstance().getInt(0, 100) < 80)
                {
                    Actor mon = new Actor(mx, my, 't', "thug", TCODColor.darkCyan);
                    mon.destruct = new MonsterDestructible(10, 0, "dead thug", engine);
                    mon.attacker = new Attacker(3);
                    mon.ai = new MonsterAI();
                    engine.actors.Add(mon);
                }
                else
                {
                    Actor mon = new Actor(mx, my, 'g', "gangster", TCODColor.darkCrimson);
                    mon.destruct = new MonsterDestructible(10, 0, "dead gangster", engine);
                    mon.attacker = new Attacker(3);
                    mon.ai = new MonsterAI();
                    engine.actors.Add(mon);
                }
            }
        }

        void hTunnel(int x1, int x2, int y) 
        {   
            int min = (x1 > x2) ? x2 : x1;
            int max = (x1 > x2) ? x1 : x2;
            for (var x = min; x <= max; x++)
            {
                tmap.setProperties(x, y, true, true);
                tiles[x + y * width].canWalk = true;
            }
        }

        void vTunnel(int y1, int y2, int x)
        {
            int min = (y1 > y2) ? y2 : y1;
            int max = (y1 > y2) ? y1 : y2;
            for (var y = min; y <= max; y++)
            {
                tmap.setProperties(x, y, true, true);
                tiles[x + y * width].canWalk = true;
            }
        }

        public void createRoom(Room room)
        {
            for (var x = room.x1 + 1; x < room.x2; x++)
            {
                for (var y = room.y1 + 1; y < room.y2; y++)
                {
                    tmap.setProperties(x, y, true, true);
                    tiles[x + y * width].canWalk = true;
                }
            }
        }
        public void endgame()
        {
            engine.wingame();
            return;
        }
    }
}
