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
            TCODImage img = new TCODImage("menu_background1.png");
            System.Diagnostics.Debug.WriteLine(Environment.CurrentDirectory.ToString());
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
        struct Message
        {
            public string text;
            public TCODColor color;
        };
        List<Message> log = new List<Message>();

        public GUI()
        {
            this.con = new TCODConsole(Globals.WIDTH, Globals.PANEL);
        }

        public void render(Engine engine)
        {
            con.setBackgroundColor(TCODColor.black);
            con.clear();
            renderBar(1, 1, Globals.BWIDTH, "HP", engine.player.destruct.hp, engine.player.destruct.maxHP, TCODColor.lightRed, TCODColor.darkerRed);

            con.print(Globals.WIDTH - 16, 0, " Revolver ");

            con.putChar(Globals.WIDTH - 12, 2, (int)TCODSpecialCharacter.Bullet);
            con.putChar(Globals.WIDTH - 13, 3, (int)TCODSpecialCharacter.Bullet);
            con.putChar(Globals.WIDTH - 11, 3, (int)TCODSpecialCharacter.Bullet);
            con.putChar(Globals.WIDTH - 13, 4, (int)TCODSpecialCharacter.Bullet);
            con.putChar(Globals.WIDTH - 11, 4, (int)TCODSpecialCharacter.Bullet);
            con.putChar(Globals.WIDTH - 12, 5, (int)TCODSpecialCharacter.Bullet);

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
