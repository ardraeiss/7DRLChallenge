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
        public bool canWalk { get { return cW; } set { cW = value; } }
        public bool isExplored { get { return explored; } set { explored = value; } }

        public Tile(bool cW, bool explored)
        {
            this.cW = cW;
            this.explored = explored;
        }
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
        bool fromload;
        Engine engine;
        TCODMap tmap;
        public Tile[] tiles;
        List<Room> rooms;
        State gameState;

        public Map(int width, int height, bool fromload, Engine engine)
        {
            this.tiles = new Tile[width * height];
            this.tmap = new TCODMap(width, height);
            this.gameState = engine.gameState;
            this.fromload = fromload;

            this.rooms = new List<Room>();

            this.width = width;
            this.height = height;
            this.engine = engine;

            if (!fromload)
            {
                genMap();
            }
            else
            {
                reGenMap(gameState.levellist[gameState.curLevel].theLayout, false, true);
            }
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
            nextLevel.portal = new Portal(this, true);
            engine.actors.Add(nextLevel);
            
            if(gameState.curLevel != 0)
            {
                Actor prevLevel = new Actor(entx, enty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false);
                engine.actors.Add(prevLevel);
            }
        }

        public void reGenMap(Tile[] level, bool forward, bool loaded = false)
        {
            if(forward) {
                engine.player.x = gameState.levellist[gameState.curLevel].startx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true);
                engine.actors.Add(nextLevel);

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false);
                engine.actors.Add(prevLevel);
            }
            else if (loaded && gameState.curLevel == 0)
            {
                engine.player.x = gameState.levellist[gameState.curLevel].startx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true);
                engine.actors.Add(nextLevel);
            }
            else if (loaded && gameState.curLevel != 0)
            {
                engine.player.x = gameState.levellist[gameState.curLevel].startx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].starty;

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false);
                engine.actors.Add(prevLevel);

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true);
                engine.actors.Add(nextLevel);
            }
            else
            {
                engine.player.x = gameState.levellist[gameState.curLevel].endx - 1;
                engine.player.y = gameState.levellist[gameState.curLevel].endy;

                Actor nextLevel = new Actor(gameState.levellist[gameState.curLevel].endx, gameState.levellist[gameState.curLevel].endy, '>', "stairs", TCODColor.sea);
                nextLevel.portal = new Portal(this, true);
                engine.actors.Add(nextLevel);

                Actor prevLevel = new Actor(gameState.levellist[gameState.curLevel].startx, gameState.levellist[gameState.curLevel].starty, '>', "stairs", TCODColor.sea);
                prevLevel.portal = new Portal(this, false);
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
                }
            }

            /// UGLY ugly function used for reloading actors and their positions
            foreach (ActorStore actordata in gameState.levellist[gameState.curLevel].actors)
            {
                if(actordata.name == "Thug" || actordata.name == "Gangster")
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
                        if (actordata.name == "Thug")
                        {
                            Actor mon = new Actor(actordata.x, actordata.y, 't', "Thug", TCODColor.darkBlue);
                            mon.destruct = new MonsterDestructible(10, 0, "dead thug", engine);
                            mon.attacker = new Attacker(3);
                            mon.ai = new MonsterAI();
                            engine.actors.Add(mon);
                        }
                        else
                        {
                            Actor mon = new Actor(actordata.x, actordata.y, 'g', "Gangster", TCODColor.darkCrimson);
                            mon.destruct = new MonsterDestructible(10, 0, "dead gangster", engine);
                            mon.attacker = new Attacker(3);
                            mon.ai = new MonsterAI();
                            engine.actors.Add(mon);
                        }
                    }
                } else if(actordata.name == "Bandage") {
                    Actor healthpot = new Actor(actordata.x, actordata.y, '!', "Bandage", TCODColor.violet);
                    healthpot.blocks = false;
                    healthpot.pick = new Healer(4);

                    engine.actors.Add(healthpot);
                }
                else if (actordata.name == "Throw rock")
                {
                    Actor confuse = new Actor(actordata.x, actordata.y, '#', "Throw rock", TCODColor.darkBlue);
                    confuse.blocks = false;
                    confuse.pick = new Confuser(10, 10, engine);

                    engine.actors.Add(confuse);
                }
                else if (actordata.name == "Gun shot"){
                    Actor light = new Actor(actordata.x, actordata.y, '#', "Gun shot", TCODColor.darkYellow);
                    light.blocks = false;
                    light.pick = new gunshot(10, 10, engine);

                    engine.actors.Add(light);
                }
                else if (actordata.name == "Fire bomb")
                {
                    Actor fire = new Actor(actordata.x, actordata.y, '#', "Fire bomb", TCODColor.darkRed);
                    fire.blocks = false;
                    fire.pick = new grenade(10, 10, engine);

                    engine.actors.Add(fire);
                }
            }

            return;
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
            TCODColor darkWall = TCODColor.darkerGrey;
            TCODColor darkGround = TCODColor.darkGrey;
            TCODColor lightWall = new TCODColor(148, 148, 148);
            TCODColor lightGround = new TCODColor(105, 105, 105);

            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {
                    if(isInView(x,y)) {
                        TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? lightWall : lightGround);
                    } else if (isExplored(x, y)) {
                        TCODConsole.root.setCharBackground(x, y, isWall(x, y) ? darkWall : darkGround);
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
                
                if (random < .01)
                {
                    Actor healthpot = new Actor(x, y, '!', "Bandage", TCODColor.violet);
                    healthpot.blocks = false;
                    healthpot.pick = new Healer(4);

                    engine.actors.Add(healthpot);
                }
                else if (random >= .02 && random < .99)
                {
                    Actor confuse = new Actor(x, y, '#', "rock", TCODColor.darkBlue);
                    confuse.blocks = false;
                    confuse.pick = new Confuser(10, 10, engine);

                    engine.actors.Add(confuse);
                }
                else if (random >= .02 && random < .01){
                    Actor gun = new Actor(x, y, '#', "ammo", TCODColor.darkYellow);
                    gun.blocks = false;
                    gun.pick = new gunshot(10, 10, engine);

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
                    Actor mon = new Actor(mx, my, 't', "Thug", TCODColor.darkCyan);
                    mon.destruct = new MonsterDestructible(10, 0, "dead thug", engine);
                    mon.attacker = new Attacker(3);
                    mon.ai = new MonsterAI();
                    engine.actors.Add(mon);
                }
                else
                {
                    Actor mon = new Actor(mx, my, 'g', "Gangster", TCODColor.darkCrimson);
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
    }
}
