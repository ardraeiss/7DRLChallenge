using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace roguelike
{

    public class Portal : Pickable
    {

        Map map;
        bool forward;

        public Portal(Map map, bool forward)
        {
            this.map = map;
            this.forward = forward;
        }

        public override bool use(Actor actor, Actor owner)
        {
            map.changeLevel(forward);
            return true;
        }
    }

    public class Girl : Pickable
    {
        Engine engine;

        public Girl(Engine engine)
        {
            this.engine = engine;
        }

        public override bool use(Actor owner, Actor wearer)
        {
            // win games
            engine.gStatus = Engine.Status.WIN;
            return true;
        }
    }

    public class Pickable
    {
        public bool pick(Actor owner, Actor wearer, Engine engine)
        {
            if (wearer.contain != null && wearer.contain.add(owner))
            {
                engine.actors.Remove(owner);
                return true;
            }

            return false;
        }

        public void drop(Actor owner, Actor wearer, Engine engine)
        {
            if (wearer.contain != null)
            {
                wearer.contain.remove(owner);
                engine.actors.Insert(0, owner);
                owner.x = wearer.x;
                owner.y = wearer.y;
                engine.gui.message(TCODColor.lightChartreuse, "{0} drops a {1}", wearer.name, owner.name);
            }
        }

        public virtual bool use(Actor owner, Actor wearer)
        {
            if (wearer.contain != null)
            {
                System.Diagnostics.Debug.WriteLine("removing {0}...", owner.name);
                wearer.contain.remove(owner);
                return true;
            }
            return false;
        }
    }

    public class Confuser : Pickable
    {
        int turnCount = 0;
        float range = 0;
        Engine engine = null;

        public Confuser(int turnCount, float range, Engine engine)
        {
            this.turnCount = turnCount;
            this.range = range;
            this.engine = engine;
        }

        public override bool use(Actor owner, Actor wearer)
        {
            int x = 0, y = 0;
            if(!engine.pickTile(ref x, ref y, range))
            {
                return false;
            }
            Actor actor = engine.getActor(x, y);
            if(actor == null)
            {
                return false;
            }

            ConfusedMonAI confusedAI = new ConfusedMonAI(turnCount, actor.ai);
            actor.ai = confusedAI;
            engine.gui.message(TCODColor.lightGreen, "{0} looks to be concused and confused", actor.name);
            return base.use(owner, actor);
        }
    }

    public class Healer : Pickable
    {
        float amount;

        public Healer(float amount)
        {
            this.amount = amount;
        }

        public override bool use(Actor owner, Actor wearer)
        {
            if (wearer.destruct != null)
            {
                
                float amountHealed = wearer.destruct.heal(amount);
                if (amountHealed > 0)
                {
                    return base.use(owner, wearer);
                }
            }
            return false;
        }
    }

    public class Lightning : Pickable
    {
        protected float range, dmg;
        protected Engine engine;

        public Lightning(float range, float dmg, Engine engine)
        {
            this.range = range;
            this.dmg = dmg;
            this.engine = engine;
        }

        public override bool use(Actor owner, Actor wearer)
        {
            Actor closestMon = engine.getClosestMon(wearer.x, wearer.y, range);
            if (closestMon == null)
            {
                engine.gui.message(TCODColor.gold, "No enemy is in range");
                return false;
            }
            engine.gui.message(TCODColor.lightBlue, "A lightning bolt strikes {0} for {1} pt of dmg", closestMon.name, dmg.ToString());
            closestMon.destruct.takeDamage(closestMon, dmg);

            return base.use(owner, wearer);
        }
    }

    public class Fireball : Lightning
    {
        private Pickable pbase = new Pickable();

        public Fireball(float range, float dmg, Engine engine) : base(range, dmg, engine) { }
        public override bool use(Actor owner, Actor wearer)
        {
            engine.gui.message(TCODColor.flame, "U/D/L/R to target tile for the fireball, enter to select, or esc to cancel.");
            int x = 0, y = 0;
            if (!engine.pickTile(ref x, ref y))
            {
                return false;
            }
            engine.gui.message(TCODColor.cyan, String.Format("The fireball explodes, burning everything within {0} tiles!", range));
            try
            {
                foreach (Actor mon in engine.actors)
                {
                    if (mon.destruct != null && !mon.destruct.isDead() && mon.getDist(x, y) <= range)
                    {
                        engine.gui.message(TCODColor.orange, String.Format("The {0} burns for {1} points of damage.", mon.name, dmg));
                        mon.destruct.takeDamage(mon, dmg);
                    }
                }
            }
            catch
            {
                // FIX THIS BUG
            }
            return pbase.use(owner, wearer);
        }
    }

    public class Container
    {
        int size;
        public List<Actor> inventory;

        public Container(int size)
        {
            this.size = size;
            this.inventory = new List<Actor>();
        }

        public bool add(Actor item)
        {
            if (size <= 0 || inventory.Count() >= size)
            {
                return false;
            }

            this.inventory.Insert(0, item);
            return true;
        }

        public void remove(Actor item)
        {
            if (inventory.Contains(item))
            {
                inventory.Remove(item);
            }
        }

    }

    public class Destructible
    {
        public float maxHP;
        public float hp;
        public float def;
        public string deadname;
        public Engine engine;

        public Destructible(float maxHP, float def, string deadname, Engine engine)
        {
            this.hp = maxHP;
            this.maxHP = maxHP;
            this.def = def;
            this.deadname = deadname;
            this.engine = engine;
        }
        
        public bool isDead()
        {
            return hp <= 0;
        }

        public float heal(float healamt)
        {
            hp += healamt;
            if (hp > maxHP)
            {
                hp = maxHP;
            }
            return healamt;
        }

        public float takeDamage(Actor owner, float dmg)
        {
            dmg -= def;
            if (dmg > 0)
            {
                hp -= dmg;
                if (hp <= 0)
                {
                    die(owner);
                }
            }
            else
            {
                dmg = 0;
            }
            return dmg;
        }

        public virtual void die(Actor owner)
        {
            owner.ch = '%';
            owner.col = TCODColor.darkRed;
            owner.blocks = false;
            engine.sendToBack(owner);
        }
    }

    public class MonsterDestructible : Destructible
    {
        public MonsterDestructible(float maxHP, float def, string deadname, Engine engine)
            : base(maxHP, def, deadname, engine)
        {
        }
        public override void die(Actor owner)
        {
            engine.gui.message(TCODColor.red, "{0} is dead!", owner.name);
            base.die(owner);
        }
    }

    public class Attacker
    {
        float atkpower;

        public Attacker(float atkpower)
        {
            this.atkpower = atkpower;
        }

        public void attack(Actor owner, Actor target, Engine engine)
        {
            if (target.destruct != null && !target.destruct.isDead())
            {
                if (atkpower - target.destruct.def > 0)
                {
                    TCODColor color = (target == engine.player) ? TCODColor.red : TCODColor.lightFlame;
                    engine.gui.message(color , "{0} attacks {1} for {2} hit points", owner.name, target.name, (atkpower - target.destruct.def).ToString());
                }
                else
                {
                    engine.gui.message(TCODColor.grey, "{0} attacks {1} but it has no effect!", owner.name, target.name);
                }
                target.destruct.takeDamage(target, atkpower);
            }
            else
            {
                // Miss, resist, etc
                engine.gui.message(TCODColor.lightGrey, "{0} attacks {1} in vain!", owner.name, target.name);
            }
        }
    }

    public abstract class AI
    {
        public abstract void update(Actor owner, Engine engine);
        public abstract void update(TCODKey key, Engine engine, Actor owner);
    }

    public class ConfusedMonAI : AI
    {
        int turnCount = 0;
        AI oldAI = null;

        public ConfusedMonAI(int turnCount, AI oldAI)
        {
            this.turnCount = turnCount;
            this.oldAI = oldAI;
        }

        public override void update(TCODKey key, Engine engine, Actor owner)
        {
            throw new NotImplementedException();
        }

        public override void update(Actor owner, Engine engine)
        {
            int dx = TCODRandom.getInstance().getInt(-1, 1);
            int dy = TCODRandom.getInstance().getInt(-1, 1);

            if (dx != 0 || dy != 0)
            {
                int destx = owner.x + dx;
                int desty = owner.y + dy;

                if (engine.map.canWalk(destx, desty))
                {
                    owner.x = destx;
                    owner.y = desty;
                }
                else
                {
                    Actor actor = engine.getActor(destx, desty);
                    if (actor != null)
                    {
                        owner.attacker.attack(owner, actor, engine);
                    }
                }
            }

            turnCount--;
            if (turnCount == 0)
            {
                owner.ai = oldAI;
            }
        }
    }

    public class MonsterAI : AI 
    {

        protected int moves;

        public override void update(TCODKey key, Engine engine, Actor owner)
        {
            throw new NotImplementedException();
        }

        public override void update(Actor owner, Engine engine)
        {
            if (owner.destruct != null && owner.destruct.isDead())
            {
                return;
            }
            if (engine.map.isInView(owner.x, owner.y))
            {
                moves = Globals.TRACKING;
            }
            else
            {
                moves--;
            }
            if (moves > 0)
            {
                moveAttack(owner, engine.player.x, engine.player.y, engine);
            }
        }

        public void moveAttack(Actor owner, int px, int py, Engine engine)
        {
            int dx = px - owner.x;
            int dy = py - owner.y;
            int stepdx = (dx > 0) ? 1 : -1;
            int stepdy = (dy > 0) ? 1 : -1;

            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist >= 2)
            {
                dx = (int)Math.Round(dx / dist);
                dy = (int)Math.Round(dy / dist);

                if (engine.map.canWalk(owner.x + dx, owner.y + dy))
                {
                    owner.x += dx;
                    owner.y += dy;
                } else if (engine.map.canWalk(owner.x+stepdx, owner.y)){
                    owner.x += stepdx;
                } else if (engine.map.canWalk(owner.x, owner.y+stepdy)){
                    owner.y += stepdy;
                }
            }

            else if (owner.attacker != null)
            {
                owner.attacker.attack(owner, engine.player, engine);
            }
        }
    }

    public class Actor
    {
        public int x, y;
        public int ch;
        public string name;
        public TCODColor col;
        public bool blocks;
        public Attacker attacker;
        public Destructible destruct;
        public AI ai;
        public Pickable pick;
        public Container contain;
        public Portal portal;

        public Actor(int x, int y, int ch, string name, TCODColor col)
        {
            this.x = x;
            this.y = y;
            this.ch = ch;
            this.col = col;
            this.name = name;
            this.blocks = true;
            this.attacker = null;
            this.destruct = null;
            this.ai = null;
            this.pick = null;
            this.contain = null;
            this.portal = null;
        }

        public void render() {
            TCODConsole.root.putChar(x, y, ch);
            TCODConsole.root.setCharForeground(x, y, col);
        }

        public void update(Engine engine)
        {
            if (ai != null) ai.update(this, engine);
        }

        public void update(TCODKey key, Engine engine)
        {
            if (ai != null) ai.update(key, engine, this);
        }

        public float getDist(int cx, int cy)
        {
            int dx = x - cx;
            int dy = y - cy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
