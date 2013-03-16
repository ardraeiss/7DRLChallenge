using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace roguelike
{
    [Serializable()]
    public class ActorStore
    {
        public int x, y;
        public string name;
        public bool dead;

        public ActorStore(int x, int y, string name, bool dead = false)
        {
            this.x = x;
            this.y = y;
            this.name = name;
            this.dead = dead;
        }
    }

    [Serializable()]
    public class SaveState
    {
        public Level[] levellist = new Level[5];
        public List<ActorStore> actorlist = new List<ActorStore>();
        public int curLevel = 0;
        public int curAmmo = 0;
        public float curHp = 0;
        public List<string> inventory = new List<string>();
        public int ammo = 0;

        public SaveState(State state, Actor player)
        {
            this.levellist = state.levellist;
            this.curAmmo = state.curAmmo;
            this.actorlist = new List<ActorStore>(state.actorlist);
            this.curLevel = state.curLevel;
            this.curHp = player.destruct.hp;
            foreach (Actor item in player.contain.inventory)
            {
                inventory.Add(item.name);
            }
        }
    }

    public class State
    {
        public Level[] levellist = new Level[5];
        public List<ActorStore> actorlist = new List<ActorStore>();
        public int curLevel = 0;
        public float curhp = 0;
        public int curAmmo = 0;
        public List<string> inventory = new List<string>();
        private LevelConverter parser;

        public State() {
            parser = new LevelConverter(levellist);
            levellist[0].isRandom = false;
            levellist[0].theLayout = parser.txtToLvl(1); 
            levellist[1].isRandom = false;
            levellist[1].theLayout = parser.txtToLvl(2); 
            levellist[2].isRandom = true;
            levellist[3].isRandom = true;
            levellist[4].isRandom = false;
            levellist[4].theLayout = parser.txtToLvl(5); 
            this.curAmmo = 6;
        }

        public State(SaveState saved)
        {
            saved.levellist.CopyTo(this.levellist, 0);
            this.actorlist = new List<ActorStore>(saved.actorlist);
            this.curLevel = saved.curLevel;
            this.curhp = saved.curHp;
            this.inventory = saved.inventory;
        }

        public Tile[] changeLvl(bool next, Tile[] prev, Engine engine, int sx, int sy, int ex, int ey, bool saving = false)
        {

            if (levellist[curLevel].isRandom)
            {
                levellist[curLevel].theLayout = prev;
                levellist[curLevel].startx = sx;
                levellist[curLevel].starty = sy;
                levellist[curLevel].endx = ex;
                levellist[curLevel].endy = ey;
                levellist[curLevel].isRandom = false;
            }

            foreach (Actor thing in engine.actors)
            {
                int x = thing.x;
                int y = thing.y;
                string tname = thing.name;
                bool isdead = false;
                if (thing.destruct != null)
                {
                    isdead = thing.destruct.isDead();
                }
                ActorStore tmpactor = new ActorStore(x, y, tname, isdead);
                actorlist.Add(tmpactor);
            }

            levellist[curLevel].actors = new List<ActorStore>(actorlist);
            actorlist.Clear();
            actorlist.TrimExcess();

            if (!saving)
            {
                if (next)
                {
                    curLevel++;
                    return getLevel(levellist[curLevel]);
                }

                curLevel--;
                return getLevel(levellist[curLevel]);
            }

            return null;
        }

        private Tile[] getLevel(Level level)
        {
            if (level.isRandom == true)
            {
                return null;
            }
            return level.theLayout;
        }

    }

    public static class Globals
    {
        // SAVE GAME
        public const string SAVE = @"assets\savegame\save.bin";

        // RESOLUTION GLOBALS
        public const int HEIGHT = 80;
        public const int WIDTH = 130;

        // ROOM GLOBALS
        public const int MAX_SIZE = 10;
        public const int MIN_SIZE = 10;
        public const int ROOMS = 50;
        public const int MAX_MON = 3;
        public const int MAX_ITEMS = 2;

        // PERCENTAGE GLOBALS
        public const double treasureP = .05;
        public const double goldP = .1;
        public const double wepP = .1;
        public const double hpP = .3;
        public const double armorP = .2;
        public const double magicP = .3;
        public const double magicthingP = .05;

        // AI GLOBALS
        public const int TRACKING = 5;

        //GUI GLOBALS
        public const int PANEL = 10;
        public const int BWIDTH = 30;
        public const int MSGX = BWIDTH + 2;
        public const int MSGHEIGHT = PANEL-1;
        public const int INV_WIDTH = 50;
        public const int INV_HEIGHT = 28;

        // STORY GLOBALS
        public static string[,] story = new string[,]
        {
            {
            "I wake up in a haze... I'm in my office, alone, with a large lump on my head.     Where's the dame, and what happened here?",
            "Bob's remains are splattered on the floor next to his desk. At least he went      out with a fight.",
            "Two thugs are waiting for me in the lobby. Chumps. I'll give 'em the old one      -two for Bobby.",
            "The entrance to our office. I should probably get outta the office building       and get some fresh air.",
            "The storage room. Always gave me the heebys",
            "Utilities and maintance. Not sure whats back this way. Smells like shit",
            "The lobby..they even got the receptionist. Poor Sally.",
            "7, no 8...10 goons near my car. I gotta find another way outta here",
            "An auxillary exit...right on time.",
            "Nothing in the closet",
            "No one in the security office. Typical."
            },
            {
            "Nothing for me this way...I'm no coward.",
            "I remember to take the girls file with me when I left. This check out of her       address. Time for a look.",
            "The only thing in the house was a used Cuban 375 Reservo cigar. I have a hunch     who is behind this",
            "She came to me scared, alone. Someone big was behind this...with a lot of man      power.",
            "Only Don Corontini fits the profile. His mansion is on the outskirts of town.     Luckily I know shortcut through the sewers",
            "Empty .....",
            "Nothing in the bedroom...except a burned out cigar...",
            "It had to be a fucking hedge maze. ",
            "A lot of noise out front; more goons, cars. Gotta split, and fast. I'll try         the back.",
            "A hedge maze....a fucking hedge maze. Here goes nothing.",
            "0"
            },
            {
                "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0"
            },
            {
                "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0"
            },
            {
            "The mansion of Don Corontini. Time to get the girl and get out. She'll probabaly      be in a back room somewhere..",
            "Leaving the wine celler and I can smell the Don's smoke already. I'll kill that        bastard",
            "The dame! Succuss tastes damn good. Now time to get us outta here",
            "Well if it isn't Larry, Curry, Moe, and...goon",
            "",
            "Cheap musk, hair gel. Getting closer",
            "Sweet freedom. Right around the corner",
            "7, no 8...10 goons near my car. I gotta find another way outta here",
            "0",
            "0",
            "Got the girl, got out. Another successful case closed.                                  THE END."
            }
        };
    }
}
