using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace PixelText
{




    class Game
    {
        public class ObjectInfo
        {
            public int X;
            public int Y;
            public int frame;
        }

        public class GameKeys
        {
            public bool kLeftDown;
            public bool kRightDown;
            public bool kUpDown;
            public bool kDownDown;
            public bool kSpaceDown;

            public GameKeys()
            {
                ReadKeysState();
            }

            public void ReadKeysState()
            {
                kLeftDown = Keyboard.IsKeyDown(Key.Left);
                kRightDown = Keyboard.IsKeyDown(Key.Right);
                kUpDown = Keyboard.IsKeyDown(Key.Up);
                kDownDown = Keyboard.IsKeyDown(Key.Down);
                kSpaceDown = Keyboard.IsKeyDown(Key.Space);
            }
        }

        private CrtTerminal crt;
        private Timer timer;
        private GameKeys gameKeys;

        public Man man;
        public Bullet[] bullets;
        public Enemy[] enemies;
        public Bomb[] bombs;
        public Barrier[] barriers;

        private int EnemiesLeft;


        public Game(CrtTerminal crt)
        {
            this.crt = crt;
            gameKeys = new GameKeys();

            man = new Man(this);

            bullets = new Bullet[2];
            for (int i = 0; i < bullets.Length; i++) bullets[i] = new Bullet(this);

            EnemiesLeft = 20;
            enemies = new Enemy[5];
            enemies[0] = new Enemy(this, 1, Enemy.ActionType.MoveRight, 0);
            enemies[1] = new Enemy(this, 3, Enemy.ActionType.MoveLeft, 3);
            enemies[2] = new Enemy(this, 5, Enemy.ActionType.MoveRight, 1);
            enemies[3] = new Enemy(this, 7, Enemy.ActionType.MoveLeft, 2);
            enemies[4] = new Enemy(this, 9, Enemy.ActionType.MoveLeft, 0);

            bombs = new Bomb[10];
            for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this);

            barriers = new Barrier[3];
            barriers[0] = new Barrier(this, 11, Barrier.ActionType.MoveLeft, 2);
            barriers[1] = new Barrier(this, 13, Barrier.ActionType.MoveRight, 4);
            barriers[2] = new Barrier(this, 15, Barrier.ActionType.MoveLeft, 0);

            timer = new Timer
            {
                Interval = 25
            };
            timer.Tick += Timer_Tick;
        }

        // Main game loop
        private void Timer_Tick(object sender, EventArgs e)
        {
            DetectBombCollistions();
            DetectBulletCollisions();

            gameKeys.ReadKeysState();

            man.TakeAction();
            man.DoMovement();
            man.Render();

            foreach (Bullet bullet in bullets)
            {
                bullet.Render();
            }

            foreach (Enemy enemy in enemies)
            {
                enemy.Render();
                enemy.DoMovement();
                if (enemy.WannaShoot())
                {
                    EnemyShoot(enemy.info.Y + 2, enemy.info.X + 3);
                }
            }

            foreach (Bomb bomb in bombs)
            {
                bomb.Render();
            }

            foreach (Barrier barrier in barriers)
            {
                barrier.Render();
                barrier.DoMovement();
            }
        }

        // Start the game
        public void Start()
        {
            timer.Enabled = true;
            this.crt.SetFocus();
        }

        // Find the free bullet slot and perform shooting
        private void ManShoot(int column)
        {
            foreach (Bullet bullet in bullets)
            {
                if (!bullet.info.shooted)
                {
                    bullet.ShootIt(column);
                    break;
                }
            }
        }

        // Find the free bomb slot and perform shooting
        private void EnemyShoot(int row, int column)
        {
            foreach (Bomb bomb in bombs)
            {
                if (!bomb.info.shooted)
                {
                    bomb.ShootIt(row, column);
                    break;
                }
            }
        }

        // Enemy was killed, resurrect if can
        private void ResurrectEnemy(Enemy enemy)
        {
            if (EnemiesLeft > 0)
            {
                Random rnd = new Random();
                EnemiesLeft--;
                if (EnemiesLeft > 4)
                {
                    Enemy.ActionType a = (rnd.Next(100) < 50 ? Enemy.ActionType.MoveLeft : Enemy.ActionType.MoveRight);
                    enemy.isAlive = true;
                    enemy.action = a;
                    enemy.info.X = (a == Enemy.ActionType.MoveLeft ? 94 : -1);
                    enemy.info.speed = rnd.Next(4);
                }
            }

            this.crt.PrintAt(0, 21, $"{EnemiesLeft} ");
        }

        // Detect collisions of bombs with man and bullets
        private void DetectBombCollistions()
        {
            foreach (Bomb bomb in bombs)
            {
                if (bomb.info.shooted)
                {
                    // collision with bullet
                    foreach (Bullet bullet in bullets)
                    {
                        if (bullet.info.shooted && bomb.info.X == bullet.info.X)
                        {
                            if (bullet.info.Y == bomb.info.Y)
                            {
                                bullet.info.shooted = bomb.info.shooted = false;
                                this.crt.PrintAt(" ", bomb.info.Y, bomb.info.X);
                            }
                        }
                    }

                    // collision with man
                    if (bomb.info.Y > 20)
                    {
                        if (bomb.info.X > man.info.X + 1 && bomb.info.X < man.info.X + 5)
                        {
                            man.ChangeLife(-1);
                            bomb.info.shooted = false; 
                        }
                    }
                }
            }
        }

        // Detect bullets collisions with enemies and barriers
        private void DetectBulletCollisions()
        {
            foreach (Bullet bullet in bullets)
            {
                if (bullet.info.shooted)
                {
                    foreach(Enemy enemy in enemies)
                    {
                        if (enemy.isAlive)
                        {
                            if (bullet.info.Y < enemy.info.Y + 2)
                                if (bullet.info.X > enemy.info.X + 1 &&
                                    bullet.info.X < enemy.info.X + 6)
                                {
                                    bullet.info.shooted = false;
                                    enemy.isAlive = false;
                                    this.crt.PrintAt("       ", enemy.info.Y + 0, enemy.info.X );
                                    this.crt.PrintAt("       ", enemy.info.Y + 1, enemy.info.X);
                                    this.crt.PrintAt(" ", bullet.info.Y, bullet.info.X);
                                    ResurrectEnemy(enemy);
                                }
                        }
                    }

                    foreach (Barrier barrier in barriers)
                    {
                        if (bullet.info.Y == barrier.info.Y)
                            if (bullet.info.X > barrier.info.X + 1 && bullet.info.X < barrier.info.X + 14)
                            {
                                bullet.info.shooted = false;
                                this.crt.PrintAt(" ", bullet.info.Y, bullet.info.X);
                            }
                    }
                }
            }
        }

        // ====================================================================
        public class Man
        {
            private Game g;
            private int shootFrameDelay;
            private const int shootDelayLimit = 3;

            public Info info;
            public ActionType action;
            public int lifes;

            public class Info : ObjectInfo
            {
                public ShootType shoot;
            }

            public enum ActionType
            {
                Stand,
                WalkRigth,
                WalkLeft,
                Shoot
            }

            public enum ShootType
            {
                ShootRight,
                ShootLeft,
                ShootStand
            }


            // Constructor
            public Man(Game game)
            {
                g = game;

                info = new Info()
                {
                    X = 0,
                    Y = 21,
                    frame = 0
                };

                action = ActionType.Stand;
                g.crt.PrintAt(0, 0, "Lives: 5    Enemies: 20    Scores: 00000");
                lifes = 5;
            }

            // Add or substract life
            public void ChangeLife(int life)
            {
                lifes += life;
                g.crt.PrintAt(0, 7, lifes.ToString());
            }

            // Detect actions of the man
            public void TakeAction()
            {
                if (action != ActionType.Shoot)
                {
                    if (action == ActionType.WalkRigth && g.gameKeys.kSpaceDown)
                    {
                        // shoot from right hand
                        shootFrameDelay = 0;
                        info.frame = 5;
                        info.shoot = ShootType.ShootRight;
                        action = ActionType.Shoot;
                    }
                    else if (action == ActionType.WalkLeft && g.gameKeys.kSpaceDown)
                    {
                        // shoot from left hand
                        shootFrameDelay = 0;
                        info.frame = -5;
                        info.shoot = ShootType.ShootLeft;
                        action = ActionType.Shoot;
                    }
                    else if (g.gameKeys.kRightDown && action != ActionType.WalkRigth)
                    {
                        // start movement right
                        info.frame = 1;
                        action = ActionType.WalkRigth;
                    }
                    else if (g.gameKeys.kLeftDown && action != ActionType.WalkLeft)
                    {
                        // start movement left
                        info.frame = -1;
                        action = ActionType.WalkLeft;
                    }
                    else if (action == ActionType.WalkRigth && !g.gameKeys.kRightDown)
                    {
                        // stop movement right
                        info.frame = 0;
                        action = ActionType.Stand;
                    }
                    else if (action == ActionType.WalkLeft && !g.gameKeys.kLeftDown)
                    {
                        // stop movement left
                        info.frame = 0;
                        action = ActionType.Stand;
                    }

                    else if (action == ActionType.Stand && g.gameKeys.kSpaceDown)
                    {
                        // shoot from both hands while standing
                        shootFrameDelay = 0;
                        info.frame = 8;
                        info.shoot = ShootType.ShootStand;
                        action = ActionType.Shoot;
                    }
                }

            }

            // Do movement of the man
            public void DoMovement()
            {
                switch (action)
                {
                    case ActionType.WalkRigth:
                        MoveManRight();
                        break;
                    case ActionType.WalkLeft:
                        MoveManLeft();
                        break;
                    case ActionType.Shoot when info.shoot == ShootType.ShootRight:
                        ShootManRight();
                        break;
                    case ActionType.Shoot when info.shoot == ShootType.ShootLeft:
                        ShootManLeft();
                        break;
                    case ActionType.Shoot when info.shoot == ShootType.ShootStand:
                        ShootManBothHands();
                        break;
                }
            }

            // Draw man sprite at screen
            public void Render()
            {
                /* 0 - спиной к игроку
                 * 1..4 - движение вправо 
                   5..7 - стрельба правой рукой в движении направо
                   8..10 - стрельба двумя руками
                   -1..-4 - движение влево
                   -5..-7 - стрельба левой рукой в движении налево*/
                if (info.frame == 0)
                {
                    // default position
                    g.crt.PrintAt(@"   o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 1)
                {
                    // default position
                    g.crt.PrintAt(@"   o-  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 2)
                {
                    // move right
                    g.crt.PrintAt(@"   o-  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  /|   ", info.Y + 2, info.X);
                }
                else if (info.frame == 3)
                {
                    // move right
                    g.crt.PrintAt(@"   o-  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 2, info.X);
                }
                else if (info.frame == 4)
                {
                    // move right
                    g.crt.PrintAt(@"   o-  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"   |\  ", info.Y + 2, info.X);
                }
                else if (info.frame == 5)
                {
                    // shoot with right hand
                    g.crt.PrintAt(@"   o-_ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 6)
                {
                    // shoot with right hand
                    g.crt.PrintAt(@"   o|  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 7)
                {
                    // shoot with right hand
                    g.crt.PrintAt(@"   o-_ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 8)
                {
                    // shoot with both hand
                    g.crt.PrintAt(@" __o__ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 9)
                {
                    // shoot with both hand
                    g.crt.PrintAt(@"  |o|  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == 10)
                {
                    // shoot with both hand
                    g.crt.PrintAt(@" __o__ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == -1)
                {
                    // default position
                    g.crt.PrintAt(@"  -o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == -2)
                {
                    // move left
                    g.crt.PrintAt(@"  -o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"   |\  ", info.Y + 2, info.X);
                }
                else if (info.frame == -3)
                {
                    // move left
                    g.crt.PrintAt(@"  -o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 2, info.X);
                }
                else if (info.frame == -4)
                {
                    // move left
                    g.crt.PrintAt(@"  -o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  /|   ", info.Y + 2, info.X);
                }
                else if (info.frame == -5)
                {
                    // shoot with left hand
                    g.crt.PrintAt(@" _-o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |   ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == -6)
                {
                    // shoot with left hand
                    g.crt.PrintAt(@"  |o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
                else if (info.frame == -7)
                {
                    // shoot with left hand
                    g.crt.PrintAt(@" _-o   ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   |\  ", info.Y + 1, info.X);
                    g.crt.PrintAt(@"  / \  ", info.Y + 2, info.X);
                }
            }

            // Shoot from right hand
            private void ShootManRight()
            {
                shootFrameDelay++;
                if (shootFrameDelay > shootDelayLimit)
                {
                    shootFrameDelay = 0;
                    info.frame++;
                    if (info.frame > 7)
                    {
                        info.frame = 0;
                        action = ActionType.Stand;
                        g.ManShoot(info.X + 4);
                    }
                }
            }

            // Shoot from left hand
            private void ShootManLeft()
            {
                shootFrameDelay++;
                if (shootFrameDelay > shootDelayLimit)
                {
                    shootFrameDelay = 0;
                    info.frame--;
                    if (info.frame < -7)
                    {
                        info.frame = 0;
                        action = ActionType.Stand;
                        g.ManShoot(info.X + 2);
                    }
                }
            }

            // Shoot from both hands
            private void ShootManBothHands()
            {
                shootFrameDelay++;
                if (shootFrameDelay > shootDelayLimit)
                {
                    shootFrameDelay = 0;
                    info.frame++;
                    if (info.frame > 10)
                    {
                        info.frame = 0;
                        action = ActionType.Stand;
                        g.ManShoot(info.X + 2);
                        g.ManShoot(info.X + 4);
                    }
                }
            }

            // Move man to the right
            private void MoveManRight()
            {
                info.frame++;

                if (info.frame > 4)
                {
                    info.frame = 1;
                }

                if (info.frame == 1 || info.frame == 2)
                {
                    info.X++;
                    if (info.X > 94)
                    {
                        info.X = 94;
                        info.frame = 0;
                        action = ActionType.Stand;
                    }
                }
            }

            // Move man to the left
            public void MoveManLeft()
            {
                info.frame--;

                if (info.frame < -4)
                {
                    info.frame = -1;
                }

                if (info.frame == -1 || info.frame == -2)
                {
                    info.X--;
                    if (info.X < 0)
                    {
                        info.X = 0;
                        info.frame = 0;
                        action = ActionType.Stand;
                    }
                }
            }
        }

        // ====================================================================
        public class Bullet
        {
            private Game g;

            public Info info;

            public class Info : ObjectInfo
            {
                public bool shooted;
            }

            public Bullet(Game game)
            {
                g = game;
                info = new Info()
                {
                    Y = 20,
                    shooted = false
                };
            }

            public void ShootIt(int column)
            {
                info.X = column;
                info.Y = 20;
                info.shooted = true;
                g.crt.PrintAt(info.Y, info.X, "'");
            }

            public void Render()
            {
                if (info.shooted)
                {
                    g.crt.PrintAt(info.Y, info.X, " ");
                    info.Y--;
                    if (info.Y < 1)
                    {
                        info.Y = 20;
                        info.shooted = false;
                    }
                    else
                    {
                        g.crt.PrintAt(info.Y, info.X, "'");
                    }
                }
            }
        }

        // ====================================================================
        public class Enemy
        {
            private Game g;
            private Random rnd;

            public Info info;
            public ActionType action;
            public bool isAlive;

            public enum ActionType
            {
                MoveRight,
                MoveLeft,
                DropBomb
            }

            public class Info : ObjectInfo
            {
                public int speed;
                public int speedLimit;
                public int frameDelay;
            }

            public Enemy(Game game, int row, ActionType atype, int speed)
            {
                g = game;
                rnd = new Random();

                action = atype;
                isAlive = true;
                info = new Info()
                {
                    Y = row,
                    X = (atype == ActionType.MoveLeft ? 94 : -1),
                    speedLimit = speed,
                    speed = 0,
                    frameDelay = 0
                };
            }

            // Return true if enemy want to shoot with probablity
            public bool WannaShoot()
            {
                if (!isAlive) return false;

                return rnd.Next(100) < 3;
            }

            // Draw enemy sprite at screen
            public void Render()
            {
                if (!isAlive) return;

                if (info.frame == 0)
                {
                    g.crt.PrintAt(@" \_*_/ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   ""   ", info.Y + 1, info.X);
                }
                else if (info.frame == 1)
                {
                    g.crt.PrintAt(@" __*__ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   ""   ", info.Y + 1, info.X);
                }
                else if (info.frame == 2)
                {
                    g.crt.PrintAt(@"  _*_  ", info.Y + 0, info.X);
                    g.crt.PrintAt(@" / "" \ ", info.Y + 1, info.X);
                }
                else if (info.frame == 3)
                {
                    g.crt.PrintAt(@" __*__ ", info.Y + 0, info.X);
                    g.crt.PrintAt(@"   ""   ", info.Y + 1, info.X);
                }
            }

            public void DoMovement()
            {
                if (!isAlive) return;

                info.speed++;

                if (info.speed > info.speedLimit)
                {
                    info.speed = 0;

                    switch (action)
                    {
                        case ActionType.MoveLeft:
                            info.X--;
                            if (info.X < -1)
                            {
                                info.X = -1;
                                action = ActionType.MoveRight;
                            }
                            break;
                        case ActionType.MoveRight:
                            info.X++;
                            if (info.X > 94)
                            {
                                info.X = 94;
                                action = ActionType.MoveLeft;
                            }
                            break;
                    }
                }

                info.frameDelay++;
                if (info.frameDelay > 2)
                {
                    info.frameDelay = 0;
                    info.frame++;
                    if (info.frame > 3) info.frame = 0;
                }
            }
        }

        // ====================================================================
        public class Bomb
        {
            private Game g;

            public Info info;

            public class Info : ObjectInfo
            {
                public bool shooted;
                public int shootDelay;
            }

            public Bomb(Game game)
            {
                g = game;

                info = new Info()
                {
                    shooted = false
                };
            }

            public void ShootIt(int row, int column)
            {
                info.X = column;
                info.Y = row;
                info.shooted = true;
                info.shootDelay = 0;
                g.crt.PrintAt(info.Y, info.X, "\"");
            }

            public void Render()
            {
                if (info.shooted)
                {
                    info.shootDelay++;
                    if (info.shootDelay > 5)
                    {
                        info.shootDelay = 0;
                        g.crt.PrintAt(info.Y, info.X, " ");
                        info.Y++;

                        if (info.Y > 23)
                        {
                            info.shooted = false;
                        }
                        else
                        {
                            g.crt.PrintAt(info.Y, info.X, "\"");
                        }
                    }
                }
            }
        }

        // ====================================================================
        public class Barrier
        {
            private Game g;

            public Info info;
            public ActionType action;

            public enum ActionType
            {
                MoveRight,
                MoveLeft
            }

            public class Info : ObjectInfo
            {
                public int speed;
                public int speedLimit;
                public int frameDelay;
            }

            public Barrier(Game game, int row, ActionType atype, int speed)
            {
                g = game;

                Random rnd = new Random();

                action = atype;
                info = new Info()
                {
                    Y = row,
                    X = rnd.Next(10, 86),
                    speedLimit = speed,
                    speed = 0,
                    frameDelay = 0
                };
            }

            // Draw enemy sprite at screen
            public void Render()
            {
                if (info.frame == 0)
                {
                    g.crt.PrintAt(@" /\/\/\/\/\/\/ ", info.Y, info.X);
                }
                else if (info.frame == 1)
                {
                    g.crt.PrintAt(@" \/\/\/\/\/\/\ ", info.Y, info.X);
                }
            }

            public void DoMovement()
            {

                info.speed++;

                if (info.speed > info.speedLimit)
                {
                    info.speed = 0;

                    switch (action)
                    {
                        case ActionType.MoveLeft:
                            info.X--;
                            if (info.X < -1)
                            {
                                info.X = -1;
                                action = ActionType.MoveRight;
                            }
                            break;
                        case ActionType.MoveRight:
                            info.X++;
                            if (info.X > 86)
                            {
                                info.X = 86;
                                action = ActionType.MoveLeft;
                            }
                            break;
                    }
                }

                info.frameDelay++;
                if (info.speed < 3)
                {
                    if (info.frameDelay > 2)
                    {
                        info.frameDelay = 0;
                        info.frame++;
                        if (info.frame > 1) info.frame = 0;
                    }
                }
                else
                {
                    if (info.frameDelay > 4)
                    {
                        info.frameDelay = 0;
                        info.frame++;
                        if (info.frame > 1) info.frame = 0;
                    }
                }
            }
        }
    }
}
