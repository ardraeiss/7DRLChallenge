using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace roguelike
{
    public class Menu
    {
        public enum MenuCode
        {
            NONE,
            NEW_GAME,
            CONTINUE,
            EXIT
        };

        public struct MenuItem
        {
            public MenuCode code;
            public string label;
        };

        public List<MenuItem> items = new List<MenuItem>();

        public void clear()
        {
            items.Clear();
        }

        public void add(MenuCode code, string label)
        {
            MenuItem item = new MenuItem();
            item.code = code;
            item.label = label;
            items.Add(item);
        }

        public MenuCode pick()
        {
            TCODImage img = new TCODImage("assets/menu_background1.png");
            int selected = 0;

            while (!TCODConsole.isWindowClosed())
            {
                img.blit2x(TCODConsole.root, 0, 0);
                int current = 0;
                foreach (MenuItem item in items)
                {
                    if (current == selected)
                    {
                        TCODConsole.root.setForegroundColor(TCODColor.lighterOrange);
                    }
                    else
                    {
                        TCODConsole.root.setForegroundColor(TCODColor.lightGrey);
                    }
                    TCODConsole.root.print(10, 10 + current * 3, item.label);
                    current++;
                }


                TCODConsole.flush();

                TCODKey key = TCODConsole.checkForKeypress();

                switch (key.KeyCode)
                {
                    case TCODKeyCode.Up:
                        selected--;
                        if (selected < 0)
                        {
                            selected = items.Count - 1;
                        }
                        break;
                    case TCODKeyCode.Down:
                        selected = (selected + 1) % items.Count;
                        break;
                    case TCODKeyCode.Enter:
                        return items[selected].code;
                    default: break;
                }
            }
            return 0;
        }
    }

    public class GUI
    {
        TCODConsole con;
        Engine engine;
        TCODImage img;
        bool init = true;

        struct Message
        {
            public string text;
            public TCODColor color;
        };
        List<Message> log = new List<Message>();

        public GUI(Engine engine)
        {
            this.engine = engine;
            this.con = new TCODConsole(Globals.WIDTH, Globals.PANEL);
            this.img = new TCODImage("assets/levels/map" + (engine.gameState.curLevel + 1) + ".png");
        }

        public void loadMapImg()
        {
            img.clear(TCODColor.black);
            img = new TCODImage("assets/levels/map" + (engine.gameState.curLevel + 1) + ".png");
        }

        public void render(Engine engine)
        {
            if (init)
            {
                loadMapImg();
                init = false;
            }
            con.setBackgroundColor(TCODColor.black);
            con.clear();
            renderBar(1, 1, Globals.BWIDTH, "HP", engine.player.destruct.hp, engine.player.destruct.maxHP, TCODColor.lightRed, TCODColor.darkerRed);
            renderCyl(engine);
            con.print(Globals.WIDTH - 18, 1, " Revolver ");
            img.blit2x(TCODConsole.root, 0, 0);
            

            int y = 1;
            float colorChanger = 0.4f;
            foreach (Message message in log)
            {
                con.setForegroundColor(message.color.Multiply(colorChanger));
                con.print(Globals.MSGX, y, message.text);
                y++;
                if(colorChanger < 1.0f) {
                    colorChanger += 0.3f;
                }
            }
            TCODConsole.blit(con, 0, 0, Globals.WIDTH, Globals.PANEL, TCODConsole.root, 0, Globals.HEIGHT - Globals.PANEL);
        }

        protected void renderCyl(Engine engine)
        {
            if (engine.gameState.curAmmo == 6)
            {
                con.putChar(Globals.WIDTH - 13, 3, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 13, 3, (int)TCODSpecialCharacter.BulletInv);
            }

            if (engine.gameState.curAmmo >= 5)
            {
                con.putChar(Globals.WIDTH - 13, 4, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 13, 4, (int)TCODSpecialCharacter.BulletInv);
            }

            if (engine.gameState.curAmmo >= 4)
            {
                con.putChar(Globals.WIDTH - 14, 5, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 14, 5, (int)TCODSpecialCharacter.BulletInv);
            }

            if (engine.gameState.curAmmo >= 3)
            {
                con.putChar(Globals.WIDTH - 15, 4, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 15, 4, (int)TCODSpecialCharacter.BulletInv);
            }

            if (engine.gameState.curAmmo >= 2)
            {
                con.putChar(Globals.WIDTH - 15, 3, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 15, 3, (int)TCODSpecialCharacter.BulletInv);
            }

            if (engine.gameState.curAmmo >= 1)
            {
                con.putChar(Globals.WIDTH - 14, 2, (int)TCODSpecialCharacter.Bullet);
            }
            else
            {
                con.putChar(Globals.WIDTH - 14, 2, (int)TCODSpecialCharacter.BulletInv);
            }
            
        }

        public void renderBar(int x, int y, int width, string name, float value, float maxValue, TCODColor bColor, TCODColor backColor)
        {
            con.setBackgroundColor(backColor);
            con.rect(x, y, width, 1, false, TCODBackgroundFlag.Set);

            int barWidth = (int)(value / maxValue * width);
            if (barWidth > 0)
            {
                con.setBackgroundColor(bColor);
                con.rect(x, y, barWidth, 1, false, TCODBackgroundFlag.Set);
            }

            con.setForegroundColor(TCODColor.white);
            con.printEx(x + width / 2, y, TCODBackgroundFlag.None, TCODAlignment.CenterAlignment, String.Format("{0} : {1} / {2}", name, value, maxValue));
        }

        public void message(TCODColor color, string maintxt, params string[] messages)
        {
            Message aMsg = new Message();
            aMsg.color = color;

            if (maintxt.Length > 75)
            {
                message(color, maintxt.Substring(75, maintxt.Length - 75).Insert(0, "-"));
                maintxt = maintxt.Substring(0, 75);
            }
            if (messages.Length > 1)
            {
                aMsg.text = String.Format(maintxt, messages);
            }
            else if (messages.Length == 1)
            {
                aMsg.text = String.Format(maintxt, messages[0]);
            }
            else
            {
                aMsg.text = maintxt;
            }
            if (log.Count() >= Globals.MSGHEIGHT)
            {
                int last = log.Count();
                Message remove = log[last-1];
                log.Remove(remove);
            }
            log.Insert(0, aMsg);
        }
    }
}
