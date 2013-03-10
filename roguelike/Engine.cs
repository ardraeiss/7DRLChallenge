using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace roguelike
{
    public class Engine
    {
        public enum Status
        {
            START,
            IDLE,
            NEWT,
            WIN,
            LOSE,
            LVLCHG
        };
        public Status gStatus;
        public Actor player;
        public List<Actor> actors = new List<Actor>();
        public Map map;
        public GUI gui;
        public Menu menu;
        public int fovRadius;
        public State gameState;
        bool fromload;

        public Engine(State gameState, bool fromload = false, State newState = null)
        {
            TCODConsole.initRoot(Globals.WIDTH, Globals.HEIGHT, "roguelike", false);
            this.fromload = fromload;

            menu = new Menu();
            menu.clear();

            menu.add(Menu.MenuCode.NEW_GAME, "New Game");
            if (fromload)
            {
                menu.add(Menu.MenuCode.CONTINUE, "Continue");
            }
            menu.add(Menu.MenuCode.EXIT, "Exit");
           
            Menu.MenuCode selection = menu.pick();

            if (selection == Menu.MenuCode.EXIT)
            {
                Environment.Exit(0);
            }
            else if (selection == Menu.MenuCode.NEW_GAME)
            {
                try
                {
                    File.Delete(Globals.SAVE);
                } catch (IOException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                fromload = false;
                if (newState == null)
                {
                    this.gameState = gameState;
                }
                else
                {
                    this.gameState = newState;
                }
            }
            else
            {
                this.gameState = gameState;
            }

            this.gStatus = Status.START;

            player = new Actor(40, 25, 64, "player", TCODColor.white);
            player.destruct = new PlayerDestructible(30, 2, "your corpse", this);
            player.ai = new PlayerAI();
            player.attacker = new Attacker(5);
            player.contain = new Container(20);
            
            if (fromload)
            {
                player.destruct.hp = gameState.curhp;
                loadInv();
            }

            actors.Add(player);
            fovRadius = 10;

            map = new Map(Globals.WIDTH, Globals.HEIGHT-Globals.PANEL, fromload, this);
            this.gui = new GUI();
        }

        public void loadInv()
        {
            foreach (String item in gameState.inventory)
            {
                if (item == "Bandage")
                {
                    Actor healthpot = new Actor(0, 0, '!', "Bandage", TCODColor.violet);
                    healthpot.blocks = false;
                    healthpot.pick = new Healer(4);

                    player.contain.inventory.Add(healthpot);
                }
                else if (item == "Throw rock")
                {
                    Actor confuse = new Actor(0, 0, '#', "Throw rock", TCODColor.darkBlue);
                    confuse.blocks = false;
                    confuse.pick = new Confuser(10, 10, this);

                    player.contain.inventory.Add(confuse);
                }
                else if (item == "Gun shot"){
                    Actor light = new Actor(0, 0, '#', "Gun shot", TCODColor.darkYellow);
                    light.blocks = false;
                    light.pick = new gunshot(10, 10, this);

                    player.contain.inventory.Add(light);
                }
                else if (item == "Fire bomb")
                {
                    Actor fire = new Actor(0, 0, '#', "Fire bomb", TCODColor.darkRed);
                    fire.blocks = false;
                    fire.pick = new grenade(10, 10, this);

                    player.contain.inventory.Add(fire);
                }
            }
        }

        public void update()
        {
            if (gStatus == Status.START) map.computeView();
            this.gStatus = Status.IDLE;

            TCODKey key = TCODConsole.checkForKeypress();

            player.update(key, this);

            if (gStatus == Status.NEWT)
            {
                try
                {
                    foreach (Actor actor in actors)
                    {
                        if (actor != player)
                        {
                            actor.update(this);
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception in update: {0}", e.Message );
                }
            }
        }

        public void render()
        {
            TCODConsole.root.clear();

            gui.render(this);
            map.render();
            

            foreach (Actor actor in actors)
            {
                if (actor != player)
                {
                    if (map.isInView(actor.x, actor.y))
                    {
                        actor.render();
                    }
                }
            }

            player.render();
        }

        public void sendToBack(Actor actor)
        {
            actors.Remove(actor);
            actors.Insert(0, actor);
        }

        public void cleanUp()
        {
            actors.Clear();
            actors.TrimExcess();
            actors.Add(player);
        }

        public Actor getClosestMon(int x, int y, float range)
        {
            Actor closest = null;
            float bestDist = 1E6f;
            float dist = 0;

            foreach (Actor mon in actors)
            {
                if (mon != player && mon.destruct != null && !mon.destruct.isDead())
                {
                    dist = mon.getDist(x, y);
                    if (dist < bestDist && (dist <= range || range == 0.0f))
                    {
                        bestDist = dist;
                        closest = mon;
                    }
                }
            }

            return closest;
        }

        public bool pickTile(ref int x, ref int y, float maxRange = 0.0f)
        {
            int chX = player.x, chY = player.y;
            bool init = true;

            while (!TCODConsole.isWindowClosed())
            {

                if (init)
                {
                    render();

                    for (int cx = 0; cx < Globals.WIDTH; cx++)
                    {
                        for (int cy = 0; cy < Globals.HEIGHT; cy++)
                        {
                            if (map.isInView(cx, cy) && (maxRange == 0 || player.getDist(cx, cy) <= maxRange) && !map.isWall(cx, cy))
                            {
                                TCODConsole.root.setCharBackground(cx, cy, TCODColor.lightGrey);
                            }
                        }
                    }

                    TCODConsole.root.setCharBackground(player.x, player.y, TCODColor.white);
                    TCODConsole.flush();
                    init = false;
                
                }

                TCODKey key = TCODConsole.waitForKeypress(false);
                switch (key.KeyCode)
                {
                    case TCODKeyCode.Left:
                        {
                            if (map.isInView(chX-1, chY) && (maxRange == 0 || player.getDist(chX-1, chY) <= maxRange) && !map.isWall(chX-1, chY))
                            {
                                TCODConsole.root.setCharBackground(chX, chY, TCODColor.lightGrey);
                                chX -= 1;
                            }
                            TCODConsole.root.setCharBackground(chX, chY, TCODColor.white);
                        }
                        break;
                    case TCODKeyCode.Right:
                        {
                            if (map.isInView(chX + 1, chY) && (maxRange == 0 || player.getDist(chX + 1, chY) <= maxRange) && !map.isWall(chX + 1, chY))
                            {
                                TCODConsole.root.setCharBackground(chX, chY, TCODColor.lightGrey);
                                chX += 1;
                            }
                            TCODConsole.root.setCharBackground(chX, chY, TCODColor.white); 
                        }
                        break;
                    case TCODKeyCode.Up:
                        {
                            if (map.isInView(chX, chY - 1) && (maxRange == 0 || player.getDist(chX, chY - 1) <= maxRange) && !map.isWall(chX, chY - 1))
                            {
                                TCODConsole.root.setCharBackground(chX, chY, TCODColor.lightGrey);
                                chY -= 1;
                            }
                            TCODConsole.root.setCharBackground(chX, chY, TCODColor.white);
                        }
                        break;
                    case TCODKeyCode.Down:
                        {
                            if (map.isInView(chX, chY + 1) && (maxRange == 0 || player.getDist(chX, chY + 1) <= maxRange) && !map.isWall(chX, chY + 1))
                            {
                                TCODConsole.root.setCharBackground(chX, chY, TCODColor.lightGrey);
                                chY += 1;
                            }
                            TCODConsole.root.setCharBackground(chX, chY, TCODColor.white);
                        }
                        break;
                    case TCODKeyCode.Enter: x = chX; y = chY; return true;
                    case TCODKeyCode.Escape: return false;
                    default: break;
                }
                TCODConsole.flush();
            }

            return false;
        }

        public Actor getActor(int x, int y)
        {
            foreach (Actor actor in actors)
            {
                if (actor.x == x && actor.y == y && actor.destruct != null && !actor.destruct.isDead())
                {
                    return actor;
                }
            }
            return null;
        }

        public void saveClose()
        {

            gameState.changeLvl(false, map.tiles, this, map.entx, map.enty, map.exx, map.exy, true);

            SaveState tosave = new SaveState(gameState, player);
            IFormatter serializer = new BinaryFormatter();
            Stream save = new FileStream(Globals.SAVE, FileMode.Create, FileAccess.Write, FileShare.None);
            serializer.Serialize(save, tosave);
            save.Close();

            Environment.Exit(0);
        }
    }
}
