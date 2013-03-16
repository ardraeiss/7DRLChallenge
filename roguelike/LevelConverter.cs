using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;
using System.IO;

namespace roguelike
{
    class LevelConverter
    {
        private int width = Globals.WIDTH;
        private int height = Globals.HEIGHT-Globals.PANEL;
        private Tile[] map;
        private Level[] levels;

        public LevelConverter(Level[] levellist)
        {
            this.levels = levellist;
        }

        public Tile[] txtToLvl(int levelnum)
        {
            map = new Tile[Globals.WIDTH * (Globals.HEIGHT - Globals.PANEL)];
            char[] textmap = getMap(levelnum);
            startMap(levelnum);
            parseMap(textmap, levelnum);
            return map;
        }

        private void startMap(int levelnum){
            levels[levelnum-1].actors = new List<ActorStore>();
        }

        private void parseMap(char[] txtmap, int levelnum)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {

                    map[x + y * width].canWalk = true;
                    map[x + y * width].isExplored = false;
                    map[x + y * width].isOutside = false;
                    map[x + y * width].prop = '0';

                    if (txtmap[x + y * width] == '.')
                    {
                        map[x + y * width].canWalk = false;
                    }
                    else if (txtmap[x + y * width] == '_')
                    {
                        map[x + y * width].isOutside = true;
                    }
                    else if (txtmap[x + y * width] == '-')
                    {
                        map[x + y * width].isOutside = true;
                        map[x + y * width].canWalk = false;
                    }
                    else if (txtmap[x + y * width] == 'p')
                    {
                        levels[levelnum - 1].startx = x;
                        levels[levelnum - 1].starty = y;
                        map[x + y * width].canWalk = true;
                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                    else if (txtmap[x + y * width] == 't')
                    {
                        map[x + y * width].canWalk = true;
                        ActorStore actor = new ActorStore(x, y, "thug");
                        levels[levelnum - 1].actors.Add(actor);

                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                    else if (txtmap[x + y * width] == 'g')
                    {
                        map[x + y * width].canWalk = true;
                        ActorStore actor = new ActorStore(x, y, "gangster");
                        levels[levelnum - 1].actors.Add(actor);

                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                    else if (txtmap[x + y * width] == 'W')
                    {
                        map[x + y * width].canWalk = true;
                        ActorStore actor = new ActorStore(x, y, "girl");
                        levels[levelnum - 1].actors.Add(actor);

                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                    else if (txtmap[x + y * width] == '>')
                    {
                        levels[levelnum - 1].endx = x;
                        levels[levelnum - 1].endy = y;
                        map[x + y * width].canWalk = true;

                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                    else
                    {
                        map[x + y * width].prop = txtmap[x + y * width];
                        if (txtmap[x + y * width + 1] == '_' || txtmap[x + y * width - 1] == '_' || txtmap[x + y * width + 1] == '-' || txtmap[x + y * width - 1] == '-')
                        {
                            map[x + y * width].isOutside = true;
                        }
                    }
                }
            }
        }

        private char[] getMap(int levelnum)
        {
            char[] textmap = new char[height*width];
            int i = 0;
            char maprow;
            StreamReader reader = new StreamReader("assets/levels/map" + levelnum.ToString() + ".txt");

            while (i < width*height)
            {
                maprow = (char)reader.Read();
                if (maprow != '\n' && maprow != '\r')
                {
                    textmap[i] = maprow;
                    i++;
                }
            }

            return textmap;
        }
    }
}
