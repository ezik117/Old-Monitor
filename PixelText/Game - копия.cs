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
        public class LocationType
        {
            public int X;
            public int Y;
            public int frame;
        }

        public enum GameActionTypes
        {
            None,
            Stop,
            Left,
            Right,
            Shoot
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


        public Game(CrtTerminal crt)
        {
            this.crt = crt;
            gameKeys = new GameKeys();

            man = new Man(this);

            timer = new Timer();
            timer.Interval = 25;
            timer.Tick += Timer_Tick;
        }

        // Main game loop
        private void Timer_Tick(object sender, EventArgs e)
        {
            man.Action();
            man.Render();
        }

        // Start the game
        public void Start()
        {
            timer.Enabled = true;
        }


        // ====================================================================
        public class Man
        {
            private Game main;
            private GameActionTypes movement;
            private GameActionTypes storedMovement;
            public ManLocationType location;
            
            public class ManLocationType : LocationType
            {
                public int shootDelay;
                public ShootType shootType;
            }

            public enum ShootType
            {
                Right,
                Left,
                Both
            }

            // Constructor
            public Man(Game main)
            {
                this.main = main;
                movement = GameActionTypes.Stop;
                storedMovement = GameActionTypes.Stop;
                location = new ManLocationType()
                {
                    X = 0,
                    Y = 21,
                    frame = 0,
                    shootDelay = 0,
                    shootType = ShootType.Right
                };
            }

            // Check and set the man action
            public void SetAction(GameActionTypes action)
            {
                Debug.WriteLine("Set action:" + action.ToString());
                switch (action)
                {
                    case GameActionTypes.Left when action != GameActionTypes.Shoot:
                        movement = action;
                        break;

                    case GameActionTypes.Right when action != GameActionTypes.Shoot:
                        movement = action;
                        break;

                    case GameActionTypes.Shoot:
                        if (movement != GameActionTypes.Stop) storedMovement = movement;

                        if (movement == GameActionTypes.Left)
                        {
                            location.frame = -4;
                            location.shootType = ShootType.Left;
                        }
                        else if (movement == GameActionTypes.Right)
                        {
                            location.frame = 4;
                            location.shootType = ShootType.Right;
                        }
                        else if (movement == GameActionTypes.Stop)
                        {
                            location.frame = 7;
                            location.shootType = ShootType.Both;
                        }
                        movement = action;
                        break;

                    case GameActionTypes.Stop when action == GameActionTypes.Shoot:
                        storedMovement = GameActionTypes.Stop;
                        break;
                    case GameActionTypes.Stop when action != GameActionTypes.Shoot: 
                        storedMovement = movement = action;
                        break;

                }
            }

            // Detect actions of the man
            public void Action()
            {
                main.gameKeys.ReadKeysState();
                if (main.gameKeys.kRightDown && movement != GameActionTypes.Right)
                {
                    SetAction(GameActionTypes.Right);
                }
                else if (main.gameKeys.kLeftDown && movement != GameActionTypes.Left)
                {
                    SetAction(GameActionTypes.Left);
                }
                else
                {
                    SetAction(GameActionTypes.Stop);
                }

                switch (movement)
                {
                    case GameActionTypes.Right:
                        MoveManRight();
                        break;

                    case GameActionTypes.Left:
                        MoveManLeft();
                        break;
                }


            }

            // Draw man sprite at screen
            public void Render()
            {
                if (location.frame == 0)
                {
                    // default position
                    main.crt.PrintAt(@"   o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 1)
                {
                    // default position
                    main.crt.PrintAt(@"   o-  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 2)
                {
                    // move right
                    main.crt.PrintAt(@"   o-  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  /|   ", location.Y + 2, location.X);
                }
                else if (location.frame == 3)
                {
                    // move right
                    main.crt.PrintAt(@"   o-  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 2, location.X);
                }
                else if (location.frame == 4)
                {
                    // move right
                    main.crt.PrintAt(@"   o-  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"   |\  ", location.Y + 2, location.X);
                }
                else if (location.frame == 5)
                {
                    // shoot with right hand
                    main.crt.PrintAt(@"   o__ ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 6)
                {
                    // shoot with right hand
                    main.crt.PrintAt(@"   o|  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 7)
                {
                    // shoot with right hand
                    main.crt.PrintAt(@"   o__ ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 8)
                {
                    // shoot with both hand
                    main.crt.PrintAt(@" __o__ ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 9)
                {
                    // shoot with both hand
                    main.crt.PrintAt(@"  |o|  ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == 10)
                {
                    // shoot with both hand
                    main.crt.PrintAt(@" __o__ ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == -1)
                {
                    // default position
                    main.crt.PrintAt(@"  -o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == -2)
                {
                    // move left
                    main.crt.PrintAt(@"  -o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"   |\  ", location.Y + 2, location.X);
                }
                else if (location.frame == -3)
                {
                    // move left
                    main.crt.PrintAt(@"  -o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 2, location.X);
                }
                else if (location.frame == -4)
                {
                    // move left
                    main.crt.PrintAt(@"  -o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"  /|\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  /|   ", location.Y + 2, location.X);
                }
                else if (location.frame == -5)
                {
                    // shoot with left hand
                    main.crt.PrintAt(@" __o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |   ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == -6)
                {
                    // shoot with left hand
                    main.crt.PrintAt(@"  |o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
                else if (location.frame == -7)
                {
                    // shoot with left hand
                    main.crt.PrintAt(@" __o   ", location.Y + 0, location.X);
                    main.crt.PrintAt(@"   |\  ", location.Y + 1, location.X);
                    main.crt.PrintAt(@"  / \  ", location.Y + 2, location.X);
                }
            }

            // Shoot from right hand
            private void ShootRight()
            {
                location.shootDelay++;
                if (location.shootDelay > 2)
                {
                    location.shootDelay = 0;
                    location.frame++;
                    if (location.frame > 6)
                    {
                        location.frame = 0;
                        movement = storedMovement;
                    }
                }
            }

            // Shoot from left hand
            private void ShootLeft()
            {
                location.shootDelay++;
                if (location.shootDelay > 2)
                {
                    location.shootDelay = 0;
                    location.frame--;
                    if (location.frame < -6)
                    {
                        location.frame = 0;
                        movement = storedMovement;
                    }
                }
            }

            // Shoot from both hand
            private void ShootBoth()
            {
                location.shootDelay++;
                if (location.shootDelay > 2)
                {
                    location.shootDelay = 0;
                    location.frame++;
                    if (location.frame > 9)
                    {
                        location.frame = 0;
                        movement = storedMovement;
                    }
                }
            }

            // Move man to the right
            private void MoveManRight()
            {
                location.frame++;
                if (location.frame == 0 || location.frame == 1)
                {
                    location.X++;
                    if (location.X > 95) location.X = 95;
                }
                else if (location.frame == 5)
                {
                    location.frame = 1;
                    location.X++;
                    if (location.X > 95) location.X = 95;
                }
            }

            // Move man to the left
            public void MoveManLeft()
            {
                location.frame--;
                if (location.frame == 0 || location.frame == -1)
                {
                    location.X--;
                    if (location.X < 0) location.X = 0;
                }
                else if (location.frame == -5)
                {
                    location.frame = -1;
                    location.X--;
                    if (location.X < 0) location.X = 0;
                }
            }
        }
    }
}
