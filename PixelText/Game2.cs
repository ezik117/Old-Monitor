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
    internal class Game2
    {
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Класс для опроса клавиш клавиатуры
        /// </summary>
        private class GameKeys
        {
            public bool kLeftDown = false;
            public bool kRightDown = false;
            public bool kUpDown = false;
            public bool kDownDown = false;
            public bool kSpaceDown = false;
            public bool kEnterDown = false;

            private Form _mainform;

            public GameKeys(Form mainform)
            {
                _mainform = mainform;
                ReadKeysState();
            }

            public void ReadKeysState()
            {
                // не считываем состояния клавиш не нашего окна
                if (Form.ActiveForm != _mainform) return;

                kLeftDown = Keyboard.IsKeyDown(Key.Left);
                kRightDown = Keyboard.IsKeyDown(Key.Right);
                kUpDown = Keyboard.IsKeyDown(Key.Up);
                kDownDown = Keyboard.IsKeyDown(Key.Down);
                kSpaceDown = Keyboard.IsKeyDown(Key.Space);
                kEnterDown = Keyboard.IsKeyDown(Key.Enter);
            }
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Базовые состояния действия спрайта
        /// </summary>
        private enum Action
        {
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            Stand,
            Shoot,
            Dead,
            Dying,
            Resurrect
        }

        /// <summary>
        /// Состояния игрового процесса. Подробнее смотри algorighm.txt
        /// </summary>
        private enum GameState
        {
            WelcomeScreen, // начальный экран игры, выбор уровня сложности
            NextLevelScreen, // заставка уровня
            NextLevelScreen_Resurrection, // мультик воскрешения человечка
            NextLevelScreen_ManIsDead, // экран когда человечек погиб
            NextLevelScreen_ManIsAlive, // экран когда человечек не погиб
            ManIsDying, // мультик рассыпания человечка
            ManIsHit, // человечка подбили
            WaitingForTheLastBomb, // ожидание падения последней бомбы

            Paused, // игра на паузе
            Running, // игра запущена
            Finished // игра завершена
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Базовыый класс параметров спрайта.
        /// Класс спрайта может наследовать данный класс и добавить параметры.
        /// </summary>
        private class SpriteInfo
        {
            /// <summary>
            /// X - координата спрайта
            /// </summary>
            public int X;

            /// <summary>
            /// Y - координата спрайта
            /// </summary>
            public int Y;

            /// <summary>
            /// Максимальное крайнее правое значение спрайта.
            /// Высчитывается как ширина терминала минус ширина спрайта + 1.
            /// Предполагает что у спрайта ширина включает крайние пробелы.
            /// </summary>
            public readonly int XLimit;

            /// <summary>
            /// Текущий номер отображаемого кадра в спрайте
            /// </summary>
            public int Frame;

            /// <summary>
            /// Счетчик количества вызовов Render() прежде чем кадр сменится
            /// </summary>
            public int FrameChangeDelay;

            /// <summary>
            /// Задает скорость перемещения спрайта, где 0-максимальная скорость.
            /// Счетчик SpeedChangeDelay должен достигнуть данного значения
            /// чтобы изменить позицию спрайта.
            /// </summary>
            public int Speed;

            /// <summary>
            /// Счетчик количества вызовов Render() прежде чем будет изменна позиция спрайта
            /// </summary>
            public int SpeedChangeDelay;

            /// <summary>
            /// Видимость спрайта.
            /// Обработка должна быть реализована самостоятельно в Render()
            /// </summary>
            public bool Visible;

            /// <summary>
            /// Переменная останавливающая изменение X,Y координат спрайта, 
            /// но не изменяющая обработку Frame. Обработка должна быть реализована
            /// самостоятельно в Move()
            /// </summary>
            public bool PauseMoving;

            /// <summary>
            /// Состояние спрайта
            /// </summary>
            public bool IsAlive;

            /// <summary>
            /// Направление движения
            /// </summary>
            public Action Action;

            /// <summary>
            /// Ширина спрайта в символах, включая крайние пробелы
            /// </summary>
            public readonly int Width;

            /// <summary>
            /// Высота спрайта в символах
            /// </summary>
            public readonly int Height;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="width">Ширина спрайта в символах с учетом крайних пробелов</param>
            /// <param name="height">Высота спрайта в символах</param>
            /// /// <param name="height">Смотри параметр XLimit</param>
            public SpriteInfo(int width, int height, int xlimit)
            {
                X = 0;
                Y = 0;
                Frame = 0;
                FrameChangeDelay = 0;
                Speed = 0;
                SpeedChangeDelay = 0;
                Action = Action.Stand;
                Visible = false;
                PauseMoving = false;
                IsAlive = true;
                Width = width;
                Height = height;
                XLimit = xlimit;
            }
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Базовые методы которые должен уметь делать спрайт
        /// </summary>
        private interface ISprite
        {
            /// <summary>
            /// Отвечает за инициализацию начальных параметров спрайта.
            /// Класс в конструкторе должен сохранить все необходимые параметры которые будут
            /// вызваны для повторной инициализации объекта
            /// </summary>
            void Init();

            /// <summary>
            /// Отвечает за вычисление перемещения спрайта
            /// </summary>
            void Move();

            /// <summary>
            /// Отвечает за отображение спрайта
            /// </summary>
            void Render();

            /// <summary>
            /// Скрывает спрайт
            /// </summary>
            void Hide();
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Базовый класс спрайта
        /// </summary>
        private abstract class Sprite : ISprite
        {
            // Параметры спрайта
            public SpriteInfo info;

            // ссылка на главный класс игры
            protected readonly Game2 game;

            // цвет спрайта
            public Color color;

            // Следующие методы должны быть переопределены в наследуемом классе

            public virtual void Move()
            {
                throw new Exception("Требуется определить метод 'Move'");
            }
            public virtual void Render()
            {
                throw new Exception("Требуется определить метод 'Render'");
            }
            public virtual void Init()
            {
                throw new Exception("Требуется определить метод 'Init'");
            }
            public virtual void Hide()
            {
                for (int r = 0; r < info.Height; r++)
                    for (int c = 0; c < info.Width; c++)
                        game.crt.PrintAt(info.Y + r, info.X + c, " ");
            }

            /// <summary>
            /// Конструктор по умолчанию. Может быть переопределен
            /// </summary>
            /// <param name="game">Ссылка на базовый класс игры</param>
            /// <param name="width">Ширина спрайта</param>
            /// <param name="height">Высота спрайта</param>
            public Sprite(Game2 game, int width, int height)
            {
                this.game = game;

                info = new SpriteInfo(width, height, game.crt.Columns - width + 1);

            }
        }

        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Объект препятствия. Пропускает бомбы врагов. Останавливает пули человечка.
        /// </summary>
        private sealed class Barrier : Sprite
        {
            // Строки с изображением спрайта
            private string image1;
            private string image2;

            // backup переменные, для использования в Init()
            private readonly int _speed;
            private readonly Action _direction;
            private readonly int _X;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="game">Ссыка на объект игры</param>
            /// <param name="width">Ширина спрайта (формирует картинку в зависимости от значения)</param>
            /// <param name="height">Высота спрайта</param>
            /// <param name="row">Ряд на котором будет двигаться спрайт</param>
            /// <param name="direction">Начальное направление</param>
            public Barrier(Game2 game, int width, int height, int speed, int row, Action direction)
                : base(game, width, height)
            {
                info.Y = row;
                _direction = info.Action = direction;
                _speed = info.Speed = speed;

                if (width % 2 == 0)
                    throw new Exception(@"Длина спрайта должна быть нечетным числом начиная с 5");

                image1 = " ";
                image2 = " ";
                for (int i = 0; i < (width-3) / 2; i++)
                {
                    image1 += @"/\";
                    image2 += @"\/";
                }
                image1 += @"/ ";
                image2 += @"\ ";

                if (direction == Action.MoveRight)
                    _X = info.X = -1;
                else
                    _X = info.X = info.XLimit;

                info.Visible = true;
                info.PauseMoving = true;

                color = Color.DeepSkyBlue;
            }

            /// <summary>
            /// Инициализация спрайта
            /// </summary>
            public override void Init()
            {
                info.X = _X;
                info.Action = _direction;
                info.Speed = _speed;
                info.Visible = true;
            }

            /// <summary>
            /// Отображениие спрайта
            /// </summary>
            public override void Render()
            {
                if (!info.Visible) return;

                if (info.Frame == 0)
                    game.crt.PrintAt(info.Y, info.X, image1, color);
                else if (info.Frame == 1)
                    game.crt.PrintAt(info.Y, info.X, image2, color);
            }

            /// <summary>
            /// Управление передвижением спрайта
            /// </summary>
            public override void Move()
            {
                if (!info.PauseMoving)
                {
                    info.SpeedChangeDelay++;

                    if (info.SpeedChangeDelay > info.Speed)
                    {
                        info.SpeedChangeDelay = 0;

                        switch (info.Action)
                        {
                            case Action.MoveLeft:
                                info.X--;
                                if (info.X < -1)
                                {
                                    info.X = -1;
                                    info.Action = Action.MoveRight;
                                }
                                break;
                            case Action.MoveRight:
                                info.X++;
                                if (info.X > info.XLimit)
                                {
                                    info.X = info.XLimit;
                                    info.Action = Action.MoveLeft;
                                }
                                break;
                        }
                    }
                }

                info.FrameChangeDelay++;
                if (info.Speed < 3)
                {
                    if (info.FrameChangeDelay > 2)
                    {
                        info.FrameChangeDelay = 0;
                        info.Frame++;
                        if (info.Frame > 1) info.Frame = 0;
                    }
                }
                else
                {
                    if (info.FrameChangeDelay > 6)
                    {
                        info.FrameChangeDelay = 0;
                        info.Frame++;
                        if (info.Frame > 1) info.Frame = 0;
                    }
                }
            }
        }


        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Объект врага.
        /// </summary>
        private sealed class Enemy : Sprite
        {
            // backup переменные, для использования в Init()
            private readonly int _speed;
            private readonly Action _direction;
            private readonly int _X;

            /// <summary>
            /// Переменная отсчитывающая количество вызовов Render() до того момента как
            /// начнется отработка алгоритма сброса бомб над головой человечка
            /// </summary>
            private int SpecialBombingDelay;

            /// <summary>
            /// Если True, то бомбардировки прекращаются
            /// </summary>
            public bool NoBombing;

            /// <summary>
            /// Если True, то враги могут менять направление движения спонтанно
            /// </summary>
            public bool Smartness;

            /// <summary>
            /// Внутренний счетчик по достужинию порога которого будет смена направления
            /// </summary>
            private int _smartnessCounter;

            /// <summary>
            /// Пороговое значение по достужинию порога которого будет смена направления
            /// </summary>
            private int _smartnessLimit;

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="game">Ссыка на объект игры</param>
            /// <param name="width">Ширина спрайта (формирует картинку в зависимости от значения)</param>
            /// <param name="height">Высота спрайта</param>
            /// <param name="row">Ряд на котором будет двигаться спрайт</param>
            /// <param name="direction">Начальное направление</param>
            public Enemy(Game2 game, int width, int height, int speed, int row, Action direction)
                : base(game, width, height)
            {
                info.Y = row;
                _direction = info.Action = direction;
                _speed = info.Speed = speed;
                info.IsAlive = true;

                if (direction == Action.MoveRight)
                    _X = info.X = -1;
                else
                    _X = info.X = info.XLimit;

                info.Visible = true;

                SpecialBombingDelay = game.rnd.Next(120, 200); // задержка от 3 до 5 секунд (3000/25...)
                NoBombing = true;
                info.PauseMoving = true;
                color = Color.Cyan;
                Smartness = false;
                _smartnessCounter = 0;
                _smartnessLimit = game.rnd.Next(80, 160); // смена направления случайна для каждых от 2 до 4 секунд
            }

            /// <summary>
            /// Инициализация спрайта
            /// </summary>
            public override void Init()
            {
                info.X = _X;
                info.Action = _direction;
                info.Speed = _speed;
                info.Visible = true;
                info.IsAlive = true;
            }

            /// <summary>
            /// Отображениие спрайта
            /// </summary>
            public override void Render()
            {
                if (SpecialBombingDelay > 0) SpecialBombingDelay--;

                if (!info.Visible) return;

                if (!info.IsAlive) return;

                if (info.Frame == 0)
                {
                    game.crt.PrintAt(@" \_*_/ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   ""   ", info.Y + 1, info.X, color);
                }
                else if (info.Frame == 1)
                {
                    game.crt.PrintAt(@" __*__ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   ""   ", info.Y + 1, info.X, color);
                }
                else if (info.Frame == 2)
                {
                    game.crt.PrintAt(@"  _*_  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@" / "" \ ", info.Y + 1, info.X, color);
                }
                else if (info.Frame == 3)
                {
                    game.crt.PrintAt(@" __*__ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   ""   ", info.Y + 1, info.X, color);
                }
            }

            /// <summary>
            /// Управление передвижением спрайта
            /// </summary>
            public override void Move()
            {
                if (!info.IsAlive) return;

                if (!info.PauseMoving)
                {
                    info.SpeedChangeDelay++;

                    if (info.SpeedChangeDelay > info.Speed)
                    {
                        info.SpeedChangeDelay = 0;

                        switch (info.Action)
                        {
                            case Action.MoveLeft:
                                info.X--;
                                if (info.X < -1)
                                {
                                    info.X = -1;
                                    info.Action = Action.MoveRight;
                                }
                                break;
                            case Action.MoveRight:
                                info.X++;
                                if (info.X > info.XLimit)
                                {
                                    info.X = info.XLimit;
                                    info.Action = Action.MoveLeft;
                                }
                                break;
                        }
                    }

                    if (Smartness)
                    {
                        _smartnessCounter++;
                        if (_smartnessCounter > _smartnessLimit)
                        {
                            _smartnessCounter = 0;
                            info.Action = (game.rnd.Next(100) < 50 ? Action.MoveLeft : Action.MoveRight);
                        }
                    }
                }

                info.FrameChangeDelay++;
                if (info.FrameChangeDelay > 2)
                {
                    info.FrameChangeDelay = 0;
                    info.Frame++;
                    if (info.Frame > 3) info.Frame = 0;
                }
            }

            /// <summary>
            /// Возвращает True, если Enemy хочет скинуть бомбу
            /// </summary>
            /// <returns></returns>
            public bool WannaShoot()
            {
                if (!info.IsAlive) return false;

                if (NoBombing) return false;

                bool result = false;

                // выстрел обязателен если человечек под Enemy
                if (SpecialBombingDelay == 0)
                {
                    if (info.X + 3 > game.man.info.X &&
                        info.X + 3 < game.man.info.X + game.man.info.Width - 1)
                        result = true;
                }

                // если человечка внизу нет, то сбрасываем бомбу по желанию
                if (!result) result = game.rnd.Next(100) < 3;

                return result;
            }

            /// <summary>
            /// Инициализирует сброс бомбы
            /// </summary>
            /// <param name="row"></param>
            /// <param name="column"></param>
            public void ShootIt()
            {
                foreach (Bomb bomb in game.bombs ?? Enumerable.Empty<Bomb>())
                {
                    if (bomb.info.Action == Action.Stand)
                    {
                        bomb.ShootIt(info.Y + 2, info.X + 3);
                        break;
                    }
                }
            }

            /// <summary>
            /// Воскрешает врага, если это возможно
            /// </summary>
            public void Resurrect()
            {
                if (game.EnemiesCount > 0)
                {
                    if (game.EnemiesCount >= game.enemies.Length)
                    {
                        Action a = (game.rnd.Next(100) < 40 ? Action.MoveLeft : Action.MoveRight);
                        info.IsAlive = true;
                        info.Action = a;
                        info.X = (a == Action.MoveLeft ? info.XLimit : -1);
                        info.Speed = game.rnd.Next(4);
                        if (info.Speed == 0)
                            color = Color.Coral;
                        else
                            color = Color.Cyan;
                    }
                }
            }
        }


        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Объект главного героя.
        /// </summary>
        private sealed class Man : Sprite
        {
            // Константы
            private const int shootDelayLimit = 3;

            // backup переменные, для использования в Init()
            private readonly Action _direction;
            private readonly int _X;

            /// <summary>
            /// Количество жизней
            /// </summary>
            public int Lives;

            /// <summary>
            /// Тип стрельбы: с правой руки, с левой руки или обеих рук
            /// </summary>
            private enum ShootType
            {
                ShootRight,
                ShootLeft,
                ShootStand
            }

            private ShootType shootType;

            private int ShootChangeDelay;

            public bool PauseShooting;
            

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="game">Ссылка на базовый класс игры</param>
            /// <param name="width">Ширина спрайта</param>
            /// <param name="height">Высота спрайта</param>
            public Man(Game2 game, int width, int height)
                : base(game, width, height)
            {
                _X = info.X = 0;
                info.Y = 21;
                _direction = info.Action = Action.Stand;
                Lives = 5;

                info.Visible = true;
                info.PauseMoving = false;
                PauseShooting = false;

                color = Color.Lime;
            }

            /// <summary>
            /// Инициализация спрайта
            /// </summary>
            public override void Init()
            {
                Lives = 5;
                info.X = _X;
                info.Action = _direction;
                info.Visible = true;
                info.IsAlive = true;
            }

            /// <summary>
            /// Отображениие спрайта
            /// </summary>
            public override void Render()
            {
                /* 0 - спиной к игроку
                   1..4 - движение вправо 
                   5..7 - стрельба правой рукой в движении направо
                   8..10 - стрельба двумя руками
                   11..19 - death
                   -1..-4 - движение влево
                   -5..-7 - стрельба левой рукой в движении налево*/
                if (!info.Visible) return;

                if (info.Frame == 0)
                {
                    // default position
                    game.crt.PrintAt(@"   o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 1)
                {
                    // default position
                    game.crt.PrintAt(@"   o-  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 2)
                {
                    // move right
                    game.crt.PrintAt(@"   o-  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  /|   ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 3)
                {
                    // move right
                    game.crt.PrintAt(@"   o-  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 4)
                {
                    // move right
                    game.crt.PrintAt(@"   o-  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"   |\  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 5)
                {
                    // shoot with right hand
                    game.crt.PrintAt(@"   o-_ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 6)
                {
                    // shoot with right hand
                    game.crt.PrintAt(@"   o|  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 7)
                {
                    // shoot with right hand
                    game.crt.PrintAt(@"   o-_ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 8)
                {
                    // shoot with both hand
                    game.crt.PrintAt(@" __o__ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 9)
                {
                    // shoot with both hand
                    game.crt.PrintAt(@"  |o|  ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 10)
                {
                    // shoot with both hand
                    game.crt.PrintAt(@" __o__ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 11)
                {
                    // death
                    game.crt.PrintAt(@"  \o/ ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@" __|__", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"      ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 12)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  \o/  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  \|/  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 13)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@" __o__ ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@" __|__ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 14)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   o   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@" _/|\_ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 15)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  o    ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@" _/|\_ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 16)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@" o/|\_ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 17)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"o _\\_ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 18)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"_ _\__ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 19)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"_ ____ ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 20)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"_ _____", info.Y + 2, info.X, color);
                }
                else if (info.Frame == 21)
                {
                    // death
                    game.crt.PrintAt(@"       ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"       ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"__RIP__", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -1)
                {
                    // default position
                    game.crt.PrintAt(@"  -o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -2)
                {
                    // move left
                    game.crt.PrintAt(@"  -o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"   |\  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -3)
                {
                    // move left
                    game.crt.PrintAt(@"  -o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -4)
                {
                    // move left
                    game.crt.PrintAt(@"  -o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"  /|\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  /|   ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -5)
                {
                    // shoot with left hand
                    game.crt.PrintAt(@" _-o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |   ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -6)
                {
                    // shoot with left hand
                    game.crt.PrintAt(@"  |o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
                else if (info.Frame == -7)
                {
                    // shoot with left hand
                    game.crt.PrintAt(@" _-o   ", info.Y + 0, info.X, color);
                    game.crt.PrintAt(@"   |\  ", info.Y + 1, info.X, color);
                    game.crt.PrintAt(@"  / \  ", info.Y + 2, info.X, color);
                }
            }

            /// <summary>
            /// В зависимости от нажатых клавиш принимает решение о поведении человечка
            /// </summary>
            public void TakeAction()
            {
                if (info.PauseMoving) return;
                if (info.Action == Action.Dying) return;

                if (info.Action != Action.Shoot)
                {
                    if (info.Action == Action.MoveRight && game.gameKeys.kSpaceDown)
                    {
                        // shoot from right hand
                        if (!PauseShooting)
                        {
                            ShootChangeDelay = 0;
                            info.Frame = 5;
                            shootType = ShootType.ShootRight;
                            info.Action = Action.Shoot;
                        }
                    }
                    else if (info.Action == Action.MoveLeft && game.gameKeys.kSpaceDown)
                    {
                        // shoot from left hand
                        if (!PauseShooting)
                        {
                            ShootChangeDelay = 0;
                            info.Frame = -5;
                            shootType = ShootType.ShootLeft;
                            info.Action = Action.Shoot;
                        }
                    }
                    else if (game.gameKeys.kRightDown && info.Action != Action.MoveRight)
                    {
                        // start movement right
                        info.Frame = 1;
                        info.Action = Action.MoveRight;
                    }
                    else if (game.gameKeys.kLeftDown && info.Action != Action.MoveLeft)
                    {
                        // start movement left
                        info.Frame = -1;
                        info.Action = Action.MoveLeft;
                    }
                    else if (info.Action == Action.MoveRight && !game.gameKeys.kRightDown)
                    {
                        // stop movement right
                        info.Frame = 0;
                        info.Action = Action.Stand;
                    }
                    else if (info.Action == Action.MoveLeft && !game.gameKeys.kLeftDown)
                    {
                        // stop movement left
                        info.Frame = 0;
                        info.Action = Action.Stand;
                    }

                    else if (info.Action == Action.Stand && game.gameKeys.kSpaceDown)
                    {
                        // shoot from both hands while standing
                        if (!PauseShooting)
                        {
                            ShootChangeDelay = 0;
                            info.Frame = 8;
                            shootType = ShootType.ShootStand;
                            info.Action = Action.Shoot;
                        }
                    }
                }
            }

            /// <summary>
            /// Управление передвижением спрайта
            /// </summary>
            public override void Move()
            {
                switch (info.Action)
                {
                    case Action.MoveRight:
                        MoveManRight();
                        break;
                    case Action.MoveLeft:
                        MoveManLeft();
                        break;
                    case Action.Shoot when shootType == ShootType.ShootRight:
                        ShootManRight();
                        break;
                    case Action.Shoot when shootType == ShootType.ShootLeft:
                        ShootManLeft();
                        break;
                    case Action.Shoot when shootType == ShootType.ShootStand:
                        ShootManBothHands();
                        break;
                    case Action.Dying:
                        Dying();
                        break;
                    case Action.Resurrect:
                        Resurrect();
                        break;
                }
            }

            /// <summary>
            /// Обработка стрельбы с правой руки
            /// </summary>
            private void ShootManRight()
            {
                ShootChangeDelay++;
                if (ShootChangeDelay > shootDelayLimit)
                {
                    ShootChangeDelay = 0;
                    info.Frame++;
                    if (info.Frame > 7)
                    {
                        info.Frame = 0;
                        info.Action = Action.Stand;
                        ShootIt(info.X + 4);
                    }
                }
            }

            /// <summary>
            /// Обработка стрельбы с левой руки
            /// </summary>
            private void ShootManLeft()
            {
                ShootChangeDelay++;
                if (ShootChangeDelay > shootDelayLimit)
                {
                    ShootChangeDelay = 0;
                    info.Frame--;
                    if (info.Frame < -7)
                    {
                        info.Frame = 0;
                        info.Action = Action.Stand;
                        ShootIt(info.X + 2);
                    }
                }
            }

            /// <summary>
            /// Обработка стрельбы с обеих рук
            /// </summary>
            private void ShootManBothHands()
            {
                ShootChangeDelay++;
                if (ShootChangeDelay > shootDelayLimit)
                {
                    ShootChangeDelay = 0;
                    info.Frame++;
                    if (info.Frame > 10)
                    {
                        info.Frame = 0;
                        info.Action = Action.Stand;
                        ShootIt(info.X + 2);
                        ShootIt(info.X + 4);
                    }
                }
            }

            /// <summary>
            /// Перемещает человечка вправо
            /// </summary>
            private void MoveManRight()
            {
                info.Frame++;

                if (info.Frame > 4)
                {
                    info.Frame = 1;
                }

                if (info.Frame == 1 || info.Frame == 2)
                {
                    info.X++;
                    if (info.X > 94)
                    {
                        info.X = 94;
                        info.Frame = 0;
                        info.Action = Action.Stand;
                    }
                }
            }

            /// <summary>
            /// Перемещает человечка влево
            /// </summary>
            private void MoveManLeft()
            {
                info.Frame--;

                if (info.Frame < -4)
                {
                    info.Frame = -1;
                }

                if (info.Frame == -1 || info.Frame == -2)
                {
                    info.X--;
                    if (info.X < 0)
                    {
                        info.X = 0;
                        info.Frame = 0;
                        info.Action = Action.Stand;
                    }
                }
            }

            /// <summary>
            /// Do dying
            /// </summary>
            private void Dying()
            {
                ShootChangeDelay++;
                if (ShootChangeDelay > 4)
                {
                    info.Frame++;

                    if (info.Frame > 21)
                    {
                        game.gameState = GameState.NextLevelScreen_ManIsDead;
                        info.Action = Action.Dead;
                        if (Lives == 0)
                        {
                            game.crt.PrintAt(20, 43, "  GAME OVER  ", Color.Yellow);
                            game.gameState = GameState.Finished;
                        }
                    }

                    ShootChangeDelay = 0;
                }
            }

            /// <summary>
            /// Do man resurrect
            /// </summary>
            private void Resurrect()
            {
                 ShootChangeDelay++;
                if (ShootChangeDelay > 4)
                {
                    info.Frame--;

                    if (info.Frame < 11)
                    {
                        info.PauseMoving = false;
                        info.Frame = 0;
                        info.Action = Action.Stand;

                        game.gameState = GameState.NextLevelScreen;
                    }

                    ShootChangeDelay = 0;
                }
            }

            /// <summary>
            /// Пытаемся выстрелить, если есть свободные слоты для стрельбы
            /// </summary>
            /// <param name="column"></param>
            private void ShootIt(int column)
            {
                foreach (Bullet bullet in game.bullets ?? Enumerable.Empty<Bullet>())
                {
                    if (bullet.info.Action == Action.Stand)
                    {
                        bullet.ShootIt(column);
                        break;
                    }
                }
            }

            /// <summary>
            /// Добавляет или убавляет количество жизней человечка
            /// </summary>
            /// <param name="life">Положительное или отрицательное значение</param>
            public void ChangeLife(int life)
            {
                Lives += life;
                game.crt.PrintAt(0, 0, $"Lives: {Lives}    Enemies: {game.EnemiesCount.ToString("D3")}    Scores: {game.Scores.ToString("D5")}    Level: {game.CurrentLevel}", Color.White);
            }
        }

        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Объект пули человечка
        /// </summary>
        private sealed class Bullet : Sprite
        {
            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="game">Ссыка на объект игры</param>
            /// <param name="width">Ширина спрайта (формирует картинку в зависимости от значения)</param>
            /// <param name="height">Высота спрайта</param>
            public Bullet(Game2 game, int width, int height)
                : base(game, width, height)
            {
                color = Color.Yellow;
                info.Visible = true;
                info.Action = Action.Stand;
            }

            /// <summary>
            /// Отображениие спрайта
            /// </summary>
            public override void Render()
            {
                if (!info.Visible) return;

                if (info.Action == Action.Shoot)
                {
                    game.crt.PrintAt(info.Y, info.X, " ");
                    info.Y--;
                    if (info.Y < 1)
                    {
                        info.Action = Action.Stand;
                    }
                    else
                    {
                        game.crt.PrintAt(info.Y, info.X, "'", color);
                    }
                }
            }

            /// <summary>
            /// Инициализация выстрела
            /// </summary>
            /// <param name="column">Позиция по X</param>
            public void ShootIt(int column)
            {
                info.X = column;
                info.Y = 21;
                info.Action = Action.Shoot;
                game.crt.PrintAt(info.Y, info.X, "'");
            }
        }


        // ----------------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Объект бомбы врагов
        /// </summary>
        private sealed class Bomb : Sprite
        {
            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="game">Ссыка на объект игры</param>
            /// <param name="width">Ширина спрайта (формирует картинку в зависимости от значения)</param>
            /// <param name="height">Высота спрайта</param>
            public Bomb(Game2 game, int width, int height)
                : base(game, width, height)
            {
                info.Visible = true;
                info.Action = Action.Stand;
                info.FrameChangeDelay = 0;
                color = Color.Pink;
            }

            /// <summary>
            /// Отображениие спрайта
            /// </summary>
            public override void Render()
            {
                if (info.Action == Action.Shoot)
                {
                    info.FrameChangeDelay++;
                    if (info.FrameChangeDelay > 5)
                    {
                        info.FrameChangeDelay = 0;
                        game.crt.PrintAt(info.Y, info.X, " ");
                        info.Y++;

                        if (info.Y > 23)
                        {
                            info.Action = Action.Stand;
                        }
                        else
                        {
                            game.crt.PrintAt(info.Y, info.X, "\"", color);
                        }
                    }
                }
            }

            /// <summary>
            /// Инициирует сброс бомбы с указанной позиции
            /// </summary>
            /// <param name="row">Y координата сброса</param>
            /// <param name="column">X координата сброса</param>
            public void ShootIt(int row, int column)
            {
                info.X = column;
                info.Y = row;
                info.Action = Action.Shoot;
                info.FrameChangeDelay = 0;
                game.crt.PrintAt(info.Y, info.X, "\"");
            }
        }

        // ----------------------------------------------------------------------------------------
        // ========================================================================================
        /// <summary>
        /// Ссылка на графический экран
        /// </summary>
        private CrtTerminal crt;

        /// <summary>
        /// Таймер игрового процесса
        /// </summary>
        private System.Windows.Forms.Timer timer;

        /// <summary>
        /// Для генерации случайных чисел
        /// </summary>
        private Random rnd = new Random();

        /// <summary>
        /// Получение состояния игровых клавиш
        /// </summary>
        private GameKeys gameKeys;

        /// <summary>
        /// Ссылка на главную форму приложения
        /// </summary>
        private Form mainform;

        /// <summary>
        /// Переключатель режимов работы игрового процесса
        /// </summary>
        private GameState gameState;

        private Man man = null;
        private Bullet[] bullets = null;
        private Barrier[] barriers = null;
        private Enemy[] enemies = null;
        private Bomb[] bombs = null;

        private int EnemiesCount;
        private int Scores;

        private int CurrentLevel;
        private int GameDifficulty = 0; //0 -easy, 1-hard
        private int blink; // для управления миганием


        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Конструктор игры
        /// </summary>
        public Game2(CrtTerminal crt, Form mainform)
        {
            this.crt = crt;

            this.mainform = mainform;

            Random rnd = new Random();
            gameKeys = new GameKeys(mainform);
            
            man = new Man(this, 7, 3);

            bullets = new Bullet[2];
            for (int i = 0; i < bullets.Length; i++) bullets[i] = new Bullet(this, 1, 1);

            timer = new System.Windows.Forms.Timer
            {
                Interval = 25,
                Enabled = false
            };
            timer.Tick += Timer_Tick;
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Запускает игровой процесс
        /// </summary>
        public void Start()
        {
            GameDifficulty = 0;
            blink = 0;
            Scores = 0;
            man.Lives = 5;
            man.info.IsAlive = true;
            man.info.Action = Action.Stand;
            man.info.Frame = 0;

            crt.ClearScreen();
            crt.SetFocus();

            CurrentLevel = 0;
            StartLevel(CurrentLevel);
        }

        // ----------------------------------------------------------------------------------------
        /// <summary>
        /// Основной игровой цикл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Enabled = false;

            // счетчик для мигания любым объектом: <0 каждые 500мс, >0 следующие 500мс
            blink++;
            if (blink > 20) blink = -21;
            if (blink == -1) blink = 0;

            // Опросим клавиатуру
            gameKeys.ReadKeysState();

            // Обработка игрового процесса
            switch (gameState)
            {
                // *** игра на паузе ***
                case GameState.Paused:
                    // мигаем
                    if (blink > 0)
                        crt.PrintAt(15, 47, " PAUSE ", Color.Yellow);
                    else
                        crt.PrintAt(15, 47, "       ");

                    // проверка выхода из режима паузы
                    if (gameKeys.kEnterDown)
                    {
                        WaitUniilKeyIsUp(Key.Enter);
                        crt.PrintAt(15, 47, "       ");
                        gameState = GameState.Running;
                    }
                    break;

                // *** экран-заставка игры ***
                case GameState.WelcomeScreen:
                    // мигаем надписью
                    if (blink > 0)
                        crt.PrintAt(15, 40, "press enter to start", Color.Violet);
                    else
                        crt.PrintAt(15, 40, "                    ");

                    // управление выбором сложности первого уровня
                    if (gameKeys.kUpDown || gameKeys.kDownDown)
                    {
                        if (GameDifficulty == 0)
                        {
                            crt.PrintAt(12, 45, "  easy", Color.White);
                            crt.PrintAt(13, 45, "> hard", Color.White);
                        }
                        else
                        {
                            crt.PrintAt(12, 45, "> easy", Color.White);
                            crt.PrintAt(13, 45, "  hard", Color.White);
                        }
                        GameDifficulty ^= 1;
                        WaitUniilKeyIsUp(Key.Up);
                        WaitUniilKeyIsUp(Key.Down);
                    }

                    // начало игры
                    if (gameKeys.kEnterDown)
                    {
                        // запускаем первый уровень
                        WaitUniilKeyIsUp(Key.Enter);
                        LevelFinished();
                        gameState = GameState.NextLevelScreen;
                    }
                    break;

                // *** экран - заставка следующего уровня, ожидаем Enter чтобы начать игру ***
                case GameState.NextLevelScreen:
                    // мигаем 
                    if (blink > 0)
                        crt.PrintAt(16, 41, "hit enter to fight", Color.Violet);
                    else
                        crt.PrintAt(16, 41, "                  ", Color.Violet);

                    man.info.PauseMoving = false;

                    // начало уровня
                    if (gameKeys.kEnterDown)
                    {
                        WaitUniilKeyIsUp(Key.Enter);
                        ClearStartScreenInfo();
                        EnableAllMonsters();
                        gameState = GameState.Running;
                    }
                    break;

                // *** проверка на последнюю упавшую бомбу ***
                case GameState.WaitingForTheLastBomb:
                    bool noActiveBombs = true;

                    foreach (Bomb bomb in bombs ?? Enumerable.Empty<Bomb>())
                        if (bomb.info.Action == Action.Shoot) noActiveBombs = false;

                    if (noActiveBombs)
                        gameState = GameState.NextLevelScreen_ManIsAlive;
                    break;

                // *** переход на следующий уровень с живым человечком ***
                case GameState.NextLevelScreen_ManIsAlive:
                    man.info.PauseMoving = false;
                    LevelFinished();
                    gameState = GameState.NextLevelScreen;
                    break;

                /// *** человечка подбили ***
                case GameState.ManIsHit:
                    man.ChangeLife(-1);
                    man.info.PauseMoving = true;
                    man.info.Action = Action.Dying;
                    man.info.Frame = 10;
                    DisableMonsters(AttackOnly: true);
                    foreach (Bomb bomb in bombs ?? Enumerable.Empty<Bomb>())
                    {
                        bomb.info.Action = Action.Stand;
                        bomb.Hide();
                    }
                    gameState = GameState.ManIsDying;
                    break;

                // *** мультик рассыпания человечка ***
                case GameState.ManIsDying:
                    // в man.Move() проверяется окончание мультика и вызывается или Finished или NextLevelScreen_ManIsDead
                    break;

                // *** запустим экран с воскрешением ***
                case GameState.NextLevelScreen_ManIsDead:
                    if (gameKeys.kEnterDown)
                    {
                        WaitUniilKeyIsUp(Key.Enter);
                        CurrentLevel--;
                        LevelFinished();
                        man.info.Action = Action.Resurrect;
                        gameState = GameState.NextLevelScreen_Resurrection;
                    }
                    break;

                // *** жизней больше нет. ожидаем Enter что бы перезапустить игру ***
                case GameState.Finished:
                    if (gameKeys.kEnterDown)
                    {
                        WaitUniilKeyIsUp(Key.Enter);
                        Start();
                    }
                    break;

                // *** игровой процесс запущен ***
                case GameState.Running:
                    // проверка на паузу
                    if (gameKeys.kEnterDown)
                    {
                        crt.PrintAt(15, 47, " PAUSE ", Color.Yellow);
                        WaitUniilKeyIsUp(Key.Enter);
                        gameState = GameState.Paused;
                    }
                    break;
            }

            if (gameState == GameState.Running)
            {
                // Определим события
                DetectBulletCollisions();
                DetectBombCollistions();
            }

            if (gameState != GameState.Paused)
            {
                // Человечек
                man.TakeAction();
                man.Move();
                man.Render();

                // Пули человечка
                foreach (Bullet bullet in bullets ?? Enumerable.Empty<Bullet>())
                {
                    bullet.Render();
                }

                // Враги
                foreach (Enemy enemy in enemies ?? Enumerable.Empty<Enemy>())
                {
                    enemy.Move();
                    enemy.Render();

                    if (enemy.WannaShoot())
                    {
                        enemy.ShootIt();
                    }
                }

                // Бомбы врагов
                foreach (Bomb bomb in bombs ?? Enumerable.Empty<Bomb>())
                {
                    bomb.Render();
                }

                // Препятствия
                foreach (Barrier barrier in barriers ?? Enumerable.Empty<Barrier>())
                {
                    barrier.Move();
                    barrier.Render();
                }
            }


            timer.Enabled = true;
        }

        /// <summary>
        /// Определяет столкновение пулей человечка с врагами и препятствиями
        /// </summary>
        private void DetectBulletCollisions()
        {
            foreach (Bullet bullet in bullets ?? Enumerable.Empty<Bullet>())
            {
                if (bullet.info.Action == Action.Shoot)
                {
                    foreach (Enemy enemy in enemies ?? Enumerable.Empty<Enemy>())
                    {
                        if (enemy.info.IsAlive)
                        {
                            if (bullet.info.Y < enemy.info.Y + 2)
                                if (bullet.info.X > enemy.info.X &&
                                    bullet.info.X < enemy.info.X + enemy.info.Width - 1)
                                {
                                    bullet.info.Action = Action.Stand;
                                    enemy.info.IsAlive = false;
                                    enemy.Hide();
                                    bullet.Hide();
                                    EnemiesCount--;
                                    enemy.Resurrect();
                                    Scores += 10;
                                    crt.PrintAt(0, 0, $"Lives: {man.Lives}    Enemies: {EnemiesCount.ToString("D3")}    Scores: {Scores.ToString("D5")}    Level: {CurrentLevel}", Color.White);

                                    if (EnemiesCount == 0)
                                        gameState = GameState.WaitingForTheLastBomb;
                                }
                        }
                    }

                    foreach (Barrier barrier in barriers ?? Enumerable.Empty<Barrier>())
                    {
                        if (bullet.info.Y == barrier.info.Y)
                            if (bullet.info.X > barrier.info.X && bullet.info.X < barrier.info.X + barrier.info.Width - 1)
                            {
                                bullet.info.Action = Action.Stand;
                                bullet.Hide();
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Определяет столкновение бомб врагов с человечком и его пулями
        /// </summary>
        private void DetectBombCollistions()
        {
            if (man.info.Action == Action.Dying || man.info.Action == Action.Dead || man.info.Action == Action.Resurrect) return;

            foreach (Bomb bomb in bombs ?? Enumerable.Empty<Bomb>())
            {
                if (bomb.info.Action == Action.Shoot)
                {
                    // collision with bullet
                    foreach (Bullet bullet in bullets ?? Enumerable.Empty<Bullet>())
                    {
                        if (bullet.info.Action == Action.Shoot && bomb.info.X == bullet.info.X)
                        {
                            if (bullet.info.Y == bomb.info.Y)
                            {
                                bullet.info.Action = bomb.info.Action = Action.Stand;
                                bomb.Hide();
                                Scores++;
                                crt.PrintAt(0, 0, $"Lives: {man.Lives}    Enemies: {EnemiesCount.ToString("D3")}    Scores: {Scores.ToString("D5")}    Level: {CurrentLevel}", Color.White);
                            }
                        }
                    }

                    // collision with man
                    if (bomb.info.Y > 20)
                    {
                        if (bomb.info.X > man.info.X + 1 &&
                            bomb.info.X < man.info.X + 6)
                        {
                            gameState = GameState.ManIsHit;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Начинает следующий уровень.
        /// </summary>
        /// <param name="level"></param>
        private void StartLevel(int level)
        {
            timer.Enabled = false;

            switch (level)
            {
                case 0:
                    crt.PrintAt(2, 1, $"tribute to ISKRA-226M, the first computer I saw and played...", Color.Yellow);
                    crt.PrintAt(9, 35, $"ALMOST THE SAME SPACE INVADERS", Color.Cyan);
                    crt.PrintAt(20, 80, $"(c)2022. A.Ermolaev.", Color.Magenta);
                    crt.PrintAt(12, 45, $"> easy", Color.White);
                    crt.PrintAt(13, 45, $"  hard", Color.White);

                    EnemiesCount = 0;
                    Scores = 0;

                    enemies = null;
                    barriers = null;
                    man.info.Visible = false;
                    man.info.X = 0;
                    
                    gameState = GameState.WelcomeScreen;
                    break;

                case 1:
                    crt.PrintAt(12, 44, $"THEY COMING !", Color.Yellow);

                    EnemiesCount = (GameDifficulty == 0 ? 50 : 100);

                    bombs = new Bomb[10]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = null;

                    man.info.Visible = true;

                    enemies = new Enemy[5];
                    enemies[0] = new Enemy(this, 7, 2, speed: 0, row: 1, Action.MoveRight); 
                    enemies[1] = new Enemy(this, 7, 2, speed: 3, row: 3, Action.MoveLeft);
                    enemies[2] = new Enemy(this, 7, 2, speed: 1, row: 5, Action.MoveRight);
                    enemies[3] = new Enemy(this, 7, 2, speed: 0, row: 7, Action.MoveLeft);
                    enemies[4] = new Enemy(this, 7, 2, speed: 2, row: 9, Action.MoveRight);
                    
                    enemies[0].color = Color.LightSalmon;
                    enemies[3].color = Color.LightSalmon;
                    break;

                case 2:
                    crt.PrintAt(12, 39, $"THEY DEFEND THEMSELVES", Color.Yellow);
                    EnemiesCount = (GameDifficulty == 0 ? 20 : 50);

                    bombs = new Bomb[10]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = new Barrier[3];
                    barriers[0] = new Barrier(this, 9, 1, speed: 1, row: 11, Action.MoveRight);
                    barriers[1] = new Barrier(this, 15, 1, speed: 1, row: 13, Action.MoveLeft);
                    barriers[2] = new Barrier(this, 31, 1, speed: 0, row: 15, Action.MoveRight);

                    enemies = new Enemy[5];
                    enemies[0] = new Enemy(this, 7, 2, speed: 0, row: 1, Action.MoveRight);
                    enemies[1] = new Enemy(this, 7, 2, speed: 0, row: 3, Action.MoveLeft);
                    enemies[2] = new Enemy(this, 7, 2, speed: 1, row: 5, Action.MoveRight);
                    enemies[3] = new Enemy(this, 7, 2, speed: 2, row: 7, Action.MoveLeft);
                    enemies[4] = new Enemy(this, 7, 2, speed: 0, row: 9, Action.MoveRight);

                    enemies[0].color = Color.LightSalmon;
                    enemies[1].color = Color.LightSalmon;
                    enemies[4].color = Color.LightSalmon;
                    break;

                case 3:
                    crt.PrintAt(12, 42, $"MORE BOMBS MORE !", Color.Yellow);
                    EnemiesCount = (GameDifficulty == 0 ? 20 : 50);

                    bombs = new Bomb[30]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = null;

                    enemies = new Enemy[5];
                    enemies[0] = new Enemy(this, 7, 2, speed: 1, row: 1, Action.MoveRight);
                    enemies[1] = new Enemy(this, 7, 2, speed: 0, row: 3, Action.MoveLeft);
                    enemies[2] = new Enemy(this, 7, 2, speed: 2, row: 5, Action.MoveRight);
                    enemies[3] = new Enemy(this, 7, 2, speed: 1, row: 7, Action.MoveLeft);
                    enemies[4] = new Enemy(this, 7, 2, speed: 0, row: 9, Action.MoveRight);

                    enemies[1].color = Color.LightSalmon;
                    enemies[4].color = Color.LightSalmon;
                    break;

                case 4:
                    crt.PrintAt(12, 38, $"THEY CHANGE THE TACTICS !", Color.Yellow);
                    EnemiesCount = (GameDifficulty == 0 ? 20 : 50);

                    bombs = new Bomb[10]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = new Barrier[5];
                    enemies = new Enemy[4];

                    enemies[0] = new Enemy(this, 7, 2, speed: 0, row: 1, Action.MoveRight);
                    barriers[0] = new Barrier(this, 9, 1, speed: 1, row: 3, Action.MoveRight);
                    enemies[0].color = Color.LightSalmon;

                    enemies[1] = new Enemy(this, 7, 2, speed: 0, row: 4, Action.MoveLeft);
                    barriers[1] = new Barrier(this, 9, 1, speed: 1, row: 6, Action.MoveLeft);
                    enemies[1].color = Color.LightSalmon;

                    enemies[2] = new Enemy(this, 7, 2, speed: 1, row: 7, Action.MoveRight);
                    barriers[2] = new Barrier(this, 11, 1, speed: 2, row: 9, Action.MoveRight);

                    enemies[3] = new Enemy(this, 7, 2, speed: 1, row: 10, Action.MoveLeft);
                    barriers[3] = new Barrier(this, 11, 1, speed: 2, row: 12, Action.MoveLeft);

                    barriers[4] = new Barrier(this, 21, 1, speed: 3, row: 15, Action.MoveRight);

                    foreach (Enemy enemy in enemies) enemy.Smartness = true;
                    break;

                case 5:
                    crt.PrintAt(12, 42, $"HOLES IN THE SKY", Color.Yellow);
                    EnemiesCount = (GameDifficulty == 0 ? 20 : 50);

                    bombs = new Bomb[10]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = new Barrier[3];
                    enemies = new Enemy[5];

                    enemies[0] = new Enemy(this, 7, 2, speed: 0, row: 1, Action.MoveRight);
                    enemies[1] = new Enemy(this, 7, 2, speed: 3, row: 4, Action.MoveLeft);
                    enemies[2] = new Enemy(this, 7, 2, speed: 1, row: 6, Action.MoveRight);
                    enemies[3] = new Enemy(this, 7, 2, speed: 2, row: 8, Action.MoveLeft);
                    enemies[4] = new Enemy(this, 7, 2, speed: 0, row: 10, Action.MoveRight);
                    enemies[0].color = Color.LightSalmon;
                    enemies[4].color = Color.LightSalmon;

                    barriers[0] = new Barrier(this, 33, 1, speed: 4, row: 14, Action.MoveRight);
                    barriers[1] = new Barrier(this, 33, 1, speed: 4, row: 15, Action.MoveLeft);
                    barriers[2] = new Barrier(this, 33, 1, speed: 0, row: 3, Action.MoveRight);
                    break;

                case 6:
                    crt.PrintAt(12, 40, $"CATCH ME IF YOU CAN", Color.Yellow);
                    EnemiesCount = (GameDifficulty == 0 ? 1 : 3);

                    bombs = new Bomb[15]; // 20, 30 хорошее количество для уровней
                    for (int i = 0; i < bombs.Length; i++) bombs[i] = new Bomb(this, 1, 1);

                    barriers = new Barrier[5];
                    enemies = new Enemy[1];

                    enemies[0] = new Enemy(this, 7, 2, speed: 0, row: 1, Action.MoveRight);
                    enemies[0].color = Color.LightSalmon;
                    enemies[0].Smartness = true;

                    barriers[0] = new Barrier(this, 31, 1, speed: 1, row: 14, Action.MoveRight);
                    barriers[1] = new Barrier(this, 31, 1, speed: 1, row: 15, Action.MoveLeft);
                    barriers[2] = new Barrier(this, 31, 1, speed: 1, row: 3, Action.MoveRight);
                    barriers[3] = new Barrier(this, 31, 1, speed: 1, row: 4, Action.MoveLeft);
                    barriers[4] = new Barrier(this, 13, 1, speed: 0, row: 9, Action.MoveRight);
                    break;

                case 7:
                    EnemiesCount = 0;
                    barriers = new Barrier[1];
                    barriers[0] = new Barrier(this, 99, 1, speed: 2, row: 20, Action.MoveRight);

                    int[] enemySpots = new int[] {
                        1,16, 3,16, 5,21, 3,26, 1,26, 7,21, // Y
                        1,36, 1,42, 1,48,   7,36, 7,42, 7,48,   3,36, 5,36,   3,48, 5,48, // O
                        1,58, 3,58, 5,58,   7,63, 7,69,   1,74, 3,74, 5,74, // U
                        11,18, 13,18, 15,18,   17,22, 15,26, 17,30,   11,34, 13,34, 15,34, // W
                        11,44, 13,44, 15,44, 17,44, // I
                        11,54, 13,54, 15,54, 17,54,    13,60, 15,64,   11,70, 13,70, 15,70, 17,70 // N
                    };

                    enemies = new Enemy[enemySpots.Length / 2];
                    for (int i=0; i<enemySpots.Length/2; i++)
                    {
                        enemies[i] = new Enemy(this, 7, 2, speed: 0, row: enemySpots[i * 2], Action.MoveRight);
                        enemies[i].color = Color.LightGreen;
                        enemies[i].info.X = enemySpots[i * 2 + 1];
                    }

                    crt.PrintAt(9, 36, "C O N G R A T U L A T I O N", Color.White);
                    crt.PrintAt(10, 16, "In the far far 80th, you could be a hero among your co-workers :)", Color.White);

                    gameState = GameState.Finished;
                    break;
            }

            if (gameState != GameState.Finished && CurrentLevel != 0)
            {
                crt.PrintAt(10, 46, $"Level {level.ToString("D2")}", Color.White);
                crt.PrintAt(16, 41, "hit enter to fight", Color.Violet);
            }

            if (CurrentLevel != 0)
                crt.PrintAt(0, 0, $"Lives: {man.Lives}    Enemies: {EnemiesCount.ToString("D3")}    Scores: {Scores.ToString("D5")}    Level: {CurrentLevel}", Color.White);

            System.Threading.Thread.Sleep(500);

            WaitUniilKeyIsUp(Key.Enter);

            DisableMonsters();

            if (CurrentLevel == 7) man.info.PauseMoving = false;

            timer.Enabled = true;
        }

        /// <summary>
        /// Очищает надписи начала уровня
        /// </summary>
        private void ClearStartScreenInfo()
        {
            crt.PrintAt(10, 46, $"        ");
            crt.PrintAt(12, 30, $"                                        ");
            crt.PrintAt(16, 41, "                  ");
        }

        /// <summary>
        /// Устанавливает атрибут PauseMoving на False для всех объектов
        /// </summary>
        private void EnableAllMonsters()
        {
            man.info.Action = Action.Stand;
            man.info.PauseMoving = false;
            man.PauseShooting = false;

            foreach (Enemy enemy in enemies ?? Enumerable.Empty<Enemy>())
            {
                enemy.info.PauseMoving = false;
                enemy.NoBombing = false;
            }

            // Препятствия
            foreach (Barrier barrier in barriers ?? Enumerable.Empty<Barrier>())
            {
                barrier.info.PauseMoving = false;
            }
        }

        /// <summary>
        /// Запуск следующего уровня
        /// </summary>
        private void LevelFinished()
        {
            crt.ClearScreen();
            CurrentLevel++;
            StartLevel(CurrentLevel);
        }

        /// <summary>
        /// Ожидает пока не будет отпущена клавиша
        /// </summary>
        /// <param name="key"></param>
        private void WaitUniilKeyIsUp(Key key)
        {
            while (Keyboard.IsKeyDown(key))
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }

        /// <summary>
        /// Отключает возможность двигаться и атаковать у всех
        /// </summary>
        private void DisableMonsters(bool AttackOnly=false)
        {
            foreach (Enemy enemy in enemies ?? Enumerable.Empty<Enemy>())
            {
                enemy.NoBombing = true;
                if (!AttackOnly) enemy.info.PauseMoving = true;
            }
            foreach (Barrier barrier in barriers ?? Enumerable.Empty<Barrier>())
                if (!AttackOnly) barrier.info.PauseMoving = true;
            man.PauseShooting = true;
            if (!AttackOnly) man.info.PauseMoving = true;
        }

        /// <summary>
        /// Включает возможность двигаться и атаковать у всех
        /// </summary>
        private void EnableMonsters()
        {
            foreach (Enemy enemy in enemies ?? Enumerable.Empty<Enemy>())
                enemy.NoBombing = enemy.info.PauseMoving = false;
            foreach (Barrier barrier in barriers ?? Enumerable.Empty<Barrier>())
                barrier.info.PauseMoving = false;
            man.PauseShooting = false;
            man.info.PauseMoving = false;
        }
    }
}
