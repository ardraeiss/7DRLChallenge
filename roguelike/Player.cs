using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace roguelike
{
    public class PlayerDestructible : Destructible
    {
        public PlayerDestructible(float maxHP, float def, string deadname, Engine engine)
            : base(maxHP, def, deadname, engine)
        {
        }
        public override void die(Actor owner)
        {
            engine.gui.message(TCODColor.red, "You died, {0}!", owner.name);
            base.die(owner);
            engine.gStatus = Engine.Status.LOSE;
        }
    }

    public class PlayerAI : AI
    {

        bool changingLevel = false;

        public override void update(Actor owner, Engine engine)
        {
            throw new NotImplementedException();
        }
        public override void update(TCODKey key, Engine engine, Actor owner)
        {
            if (owner.destruct != null && owner.destruct.isDead())
            {
                return;
            }

            int dx = 0, dy = 0;
            switch (key.KeyCode)
            {
                case TCODKeyCode.Up: dy = -1; break;
                case TCODKeyCode.Down: dy = 1; break;
                case TCODKeyCode.Left: dx = -1; break;
                case TCODKeyCode.Right: dx = 1; break;
                case TCODKeyCode.Escape:
                    {
                        engine.saveClose();
                        break;
                    }
                case TCODKeyCode.Char: handleOtherKeys(owner, key.Character, engine); break;
                default: break;
            }
            
            if (dx != 0 || dy != 0)
            {
                engine.gStatus = Engine.Status.NEWT;
                if (moveAttack(owner, owner.x + dx, owner.y + dy, engine))
                {
                    engine.map.computeView();
                }
            }
        }

        public Actor inventory(Actor player)
        {
            TCODConsole con = new TCODConsole(Globals.INV_WIDTH, Globals.INV_HEIGHT);
            con.setForegroundColor(new TCODColor(200,180,150));
            con.printFrame(0, 0, Globals.INV_WIDTH, Globals.INV_HEIGHT, true, TCODBackgroundFlag.Default, "Backpack");

            int y = 1;
            char shortcut = 'a';
            int itemIndex = 0;

            foreach(Actor item in player.contain.inventory)
            {
                con.print(2, y, String.Format("({0}) {1}", shortcut, item.name));
                y++;
                shortcut++;
            }
            TCODConsole.blit(con, 0, 0, Globals.INV_WIDTH, Globals.INV_HEIGHT, TCODConsole.root, Globals.WIDTH / 2 - Globals.INV_WIDTH / 2, Globals.HEIGHT / 2 - Globals.INV_HEIGHT / 2);
            TCODConsole.flush();

            TCODKey key = TCODConsole.waitForKeypress(false);
           
            if (key.Character >= 97 && key.Character <= 122)
            {
                itemIndex = key.Character - 'a';

                if (itemIndex >= 0 && itemIndex < player.contain.inventory.Count())
                {
                    return player.contain.inventory[itemIndex];
                }
            }
            return null;
        }

        public void handleOtherKeys(Actor player, char keycode, Engine engine)
        {
            switch (keycode)
            {
                case 'g':
                    {
                        bool found = false;
                        foreach (Actor actor in engine.actors)
                        {
                            if (actor.pick != null && actor.x == player.x && actor.y == player.y)
                            {
                                if (actor.pick.pick(actor, player, engine))
                                {
                                    found = true;
                                    engine.gui.message(TCODColor.silver, "You pick up a {0}", actor.name);
                                    break;
                                }
                                else if (!found)
                                {
                                    found = true;
                                    engine.gui.message(TCODColor.red, "Your inventory is full!");
                                }
                            }
                        }
                        if (!found)
                        {
                            engine.gui.message(TCODColor.lightGrey, "There's nothing here to find.");
                        }
                    }
                    break;
                case 'i':
                    {
                        Actor item = inventory(player);
                        if (item != null)
                        {
                            item.pick.use(item, player);
                            engine.gStatus = Engine.Status.NEWT;
                        }
                    }
                    break;
                case 'd':
                    {
                        Actor actor = inventory(player);
                        if(actor != null)
                        {
                            actor.pick.drop(actor, player, engine);
                            engine.gStatus = Engine.Status.NEWT;
                        }
                    }
                    break;
                default: break;
            }
        }

        public bool moveAttack(Actor owner, int tarx, int tary, Engine engine)
        {
            if (engine.map.isWall(tarx, tary)) return false;
            try
            {
                foreach (Actor actor in engine.actors)
                {
                    if (actor.destruct != null && !actor.destruct.isDead() && actor.x == tarx && actor.y == tary)
                    {
                        owner.attacker.attack(owner, actor, engine);
                        return false;
                    }
                    else if (((actor.destruct != null && actor.destruct.isDead()) || actor.pick != null) && actor.x == tarx && actor.y == tary)
                    {
                        engine.gui.message(TCODColor.lightGrey, "There's a(n) {0} here", actor.name);
                    }
                    else if (actor.portal != null && actor.x == tarx && actor.y == tary)
                    {
                        changingLevel = true;
                        actor.portal.use(owner, actor);
                        engine.gui.message(TCODColor.purple, "You go down the stairs...");
                        break;
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine("Exception in player moveAttack: {0}", e.Message);
            }

            if (!changingLevel)
            {
                owner.x = tarx;
                owner.y = tary;
                return true;
            }

            changingLevel = false;
            return true;
        }

    }
}
