using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace IdealGasSimulator
{
    public partial class MainForm : Form
    {
        // Переменные для параметров модели:
        int N;              //  Количество частиц
        double R;           //  Радиус частиц
        double Vmax;        //  Параметр для начальной скорости частиц
        double Xmax;        //  1/2 длины области
        double Ymax;        //  1/2 ширины области
        double dt;          //  Временной шаг
        double K;           //  Масштабный коэффициент
        double Ekin0;       //  Начальное значение удельной кинетической энергии системы
        double Ekin;        //  Текущее значение удельной кинетической энергии системы
        int t;              //  Счетчик времени (количество шагов)
        double[] X;         //  Массив для координаты X частиц
        double[] Y;         //  Массив для координаты Y частиц
        double[] Vx;        //  Массив для проекции Vx скорости частиц на координатную ось Ox    
        double[] Vy;        //  Массив для проекции Vy скорости частиц на координатную ось Oy   
        double eps;         //  Допустимая погрешность при расчете суммарной кинетической энергии системы

        // Переменные для обработки столкновения частиц между собой:
        double Px, Py, P2;                //  Переменные для координат и квадрата модуля вектора, соединяющего центры двух столкнувшихся частиц
        double VPix, VPiy, VNix, VNiy;    //  Переменные для координат параллельной и нормальной составляющих скорости одной из столкнувшихся частиц 
        double VPjx, VPjy, VNjx, VNjy;    //  Переменные для координат параллельной и нормальной составляющих скорости второй из столкнувшихся частиц

        // Переменная-индикатор, показывающая будет ли движение новой модели или продолжение прежней модели с того момента, где движение было остановлено
        bool NewModel = true;

        // Переменная-индикатор, показывающая было ли сохранение последнего состояния модели в файл
        bool SaveModel = false;

        // Объект для генерации случайных чисел:
        Random Rnd = new Random();

        // Объекты для графики:
        Bitmap BM;                                      //  Объект для хранения рисунка
        Graphics Gr;                                    //  Объект-инструмент для рисования на BM
        Pen Pen1 = new Pen(Color.Red, 2);               //  Карандаш красного цвета для объекта Gr
        Pen Pen2 = new Pen(Color.Blue, 2);              //  Карандаш синего цвета для объекта Gr  
        SolidBrush Br = new SolidBrush(Color.Yellow);   //  Кисть желтого цвета для объекта Gr     

        // Объекты для записи и чтения данных из файла:
        FileStream f;           // Объект для передачи данных в(из) файл(а)
        BinaryWriter DataIn;    // Более совершенный объект для записи данных в файл
        BinaryReader DataOut;   // Более совершенный объект для чтения данных из файла

        // Объекты для работы с формами:
        public static MainForm Fr1;
        public static StatisticsForm Fr2;

        // Переменные для средних значений:
        double Vsr;   // Средняя скорость
        double Lsr;   // Средняя длина свободного пробега
        double Nsr;   // Средняя частота соударений
        public static double V2sr;  // Средний квадрат скорости
        double[] CS;  // Массив счетчиков пройденного пути
        int[] CN;     // Массив счетчиков количества соударений  

        // Переменные для распределения Максвелла:
        public static double dV;    // Интервал скорости в распределении Максвелла
        public static int NV;       // Количество интервалов в распределении Максвелла
        public static int TimeV;    // Количество шагов, через которое частицы добавляются в распределение Максвелла  
        public static int[] CV;     // Массив счетчиков для частиц, попавших в соответствующие интервалы  


        public MainForm()
        {
            InitializeComponent();
        }
        
        //  Процедура выполняется в самом начале при запуске программы
        private void Form1_Load(object sender, EventArgs e)   
        {
            // Объект для обращения к главной форме MainForm
            Fr1 = this;

            // Создание объектов для графики. Размер рисунка в BM такой же, как и в pictureBox1
            BM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Gr = Graphics.FromImage(BM);
        }
        


        //  Процедура выполняется при изменении размеров окна программы (вместе с этим и размеров компонента pictureBox1)
        private void pictureBox1_Resize(object sender, EventArgs e)   
        {

            if ((pictureBox1.Width == 0) || (pictureBox1.Height == 0)) return;   //  Преждевременный выход из программы, если окно свернуто и pictureBox1 имеет нулевые размеры

            // Создание объектов для графики. Размер рисунка в BM такой же, как и в pictureBox1
            BM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Gr = Graphics.FromImage(BM);

            timer2.Enabled = true;  //  По техническим причинам перерисовать рисунок здесь не получится. Поэтому, включаем вспомогательный timer2, где рисунок перерисовывается и сразу timer2 выключается.

        }



        //  Процедура таймера timer2. Нужна только для перерисовки рисунка, который по техническим причинам перерисовывается именно здесь. 
        private void timer2_Tick(object sender, EventArgs e)
        {
            DrawPicture();            //  перерисовать рисунок  
            timer2.Enabled = false;   //  выключить таймер
        }
        


        //  Процедура выполняется при установки или снятии значка в элементе checkBox1
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            DrawPicture();      //  перерисовать рисунок 
        }



        //  Процедура выполняется при вращении колеса мыши на элементе pictureBox1
        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (NewModel) return;                // Принудительный выход из процедуры, если модель не открыта (это нужно, чтобы избежать исключительные ситуации)
            K *= Math.Pow(1.1, e.Delta / 100);   // Изменение масштабного коэффициента
            textBox7.Text = string.Format("{0:f2}", K);  // Вывод значения масштабного коэффициента в соответствующий textBox
            DrawPicture();  // Перерисовка рисунка
        }
        


        //   Процедура выполняется при прямом вызове из других процедур по ее имени. Здесь формируется рисунок на pictureBox1.
        private void DrawPicture()
        {
            
            // Рисунок:
            Gr.Clear(pictureBox1.BackColor);  // стереть старый рисунок
            if (checkBox1.Checked)          //  Если установлен значок в элементе checkBox1, то нарисовать область с частицами (иначе рисунок останется пустым)
            {
                Gr.DrawRectangle(Pen2, (int)(BM.Width / 2 - K * Xmax), (int)(BM.Height / 2 - K * Ymax), (int)(2 * Xmax * K), (int)(2 * Ymax * K));  // нарисовать область движения (прямоугольник)
                for (int i = 0; i < N; i++)   // нарисовать частицы в виде закрашенных кругов
                {
                    Gr.DrawEllipse(Pen1, (int)(BM.Width / 2 + K * (X[i] - R)), (int)(BM.Height / 2 - K * (Y[i] + R)), (int)(2 * R * K), (int)(2 * R * K));
                    Gr.FillEllipse(Br, (int)(BM.Width / 2 + K * (X[i] - R)), (int)(BM.Height / 2 - K * (Y[i] + R)), (int)(2 * R * K), (int)(2 * R * K));                   
                }
            }

            pictureBox1.Image = BM;  // перебросить рисунок из BM на pictureBox1

        }



        //  Процедура выполняется при каждом нажатии кнопки ПУСК (button1)
        private void button1_Click(object sender, EventArgs e)   
        {
            int Ntb = 0;   //  Вспомогательная переменная. Сюда записывается номер textBox-а, в котором произошла исключительная ситуация. Затем на этот textBox будет наведен фокус ввода с выделением текста.

            try  // Проверка на исключительные ситуации
            {

                if (NewModel)    // Если новая модель, то ...
                {
                    //  Запись введенных значений параметров из textBox-ов в соответствующие переменные.
                    //  Если значение будет логически некорректным, то возникнет или будет сгенерирована принудительно исключительная ситуация с последующей обработкой в блоке catch ниже 
                    Ntb = 1;    N = int.Parse(textBox1.Text);           if (N <= 0) throw new Exception("Количество частиц <= 0.");
                    Ntb = 2;    R = double.Parse(textBox2.Text);        if (R <= 0) throw new Exception("Радиус частиц <= 0.");
                    Ntb = 3;    Vmax = double.Parse(textBox3.Text);     if (Vmax <= 0) throw new Exception("Начальная скорость частиц <= 0.");
                    Ntb = 4;    Xmax = double.Parse(textBox4.Text) / 2; if (Xmax <= 0) throw new Exception("Длина области <= 0.");
                    Ntb = 5;    Ymax = double.Parse(textBox5.Text) / 2; if (Ymax <= 0) throw new Exception("Ширина области <= 0.");
                    Ntb = 6;    dt = double.Parse(textBox6.Text);       if (dt <= 0) throw new Exception("Временной шаг <= 0.");
                    Ntb = 9;    dV = double.Parse(textBox9.Text);       if (dV <= 0) throw new Exception("Интервал скоростей в распределении <= 0.");
                    Ntb = 10;   NV = int.Parse(textBox10.Text);         if (NV <= 0) throw new Exception("Количество интервалов в распределении <= 0.");
                    Ntb = 11;   TimeV = int.Parse(textBox11.Text);      if (TimeV <= 0) throw new Exception("Интервал времени добавления частиц <= 0.");

                    //   Создание массивов для координат и проекций скорости частиц. Размер массивов равен количеству частиц N:
                    X = new double[N];
                    Y = new double[N];
                    Vx = new double[N];
                    Vy = new double[N];

                    //   Создание массивов счетчиков пройденного пути и количества соударений. Размер массивов равен количеству частиц N:
                    CS = new double[N];
                    CN = new int[N];

                    //   Создание массива счетчиков количества частиц, попавших в соответствующие интервалы в распределении Максвелла. Размер массива равен количеству интервалов NV:
                    CV = new int[NV];

                    //   Заполнение массивов случайным образом (используется объект Rnd для генерации случайных чисел),
                    //   а также расчет начальной удельной кинетической энергии системы,
                    //   и еще обнуление счетчиков пройденного пути и количества соударений для каждой частицы
                    Ekin0 = 0;
                    for (int i = 0; i < N; i++)
                    {
                        X[i] = (2 * Rnd.NextDouble() - 1) * (Xmax - R);   //  координата X для i-й частицы
                        Y[i] = (2 * Rnd.NextDouble() - 1) * (Ymax - R);   //  координата Y для i-й частицы
                        Vx[i] = (2 * Rnd.NextDouble() - 1) * Vmax;        //  проекция скорости Vx для i-й частицы
                        Vy[i] = (2 * Rnd.NextDouble() - 1) * Vmax;        //  проекция скорости Vy для i-й частицы
                        Ekin0 += Vx[i] * Vx[i] + Vy[i] * Vy[i];      //  суммирование удельной кинетической энергии частиц (разделим на 2 потом всю сумму)  
                        CS[i] = 0;     //   установить в ноль счетчик пройденного пути i-й частицы 
                        CN[i] = 0;     //   установить в ноль счетчик количества соударений i-й частицы
                    }
                    Ekin0 /= 2;        //  суммарная кинетическая энергия системы
                    textBox12.Text = string.Format("{0:f8}", Ekin0);
                    eps = Ekin0 * 1E-12;   //  устанавливается значение допустимой погрешности при расчете суммарной кинетической энергии системы

                    //   Обнулуние счетчиков частиц в распределении Максвелла:
                    for (int i = 0; i < NV; i++)
                    {
                        CV[i] = 0;     //   установить в ноль счетчик количества частиц, попавших в i-й интервал
                    }

                    t = 0;             //   установить в ноль счетчик времени
                    textBox8.Text = "0";

                    NewModel = false;  //   чтобы при следующем нажатии кнопки ПУСК модель не считалась новой
                }


                //  Запись значения масштабного коэффициента из textBox6 в переменную K. Это нужно сделать, так как масштабный коэффициент мог быть изменен.
                Ntb = 7;    K = double.Parse(textBox7.Text);    if (K <= 0) throw new Exception("Масштабный коэффициент <= 0.");

                // Блокировка и разблокировка кнопок:
                button1.Enabled = false;
                button2.Enabled = true;
                открытьToolStripMenuItem.Enabled = false;
                сохранитьToolStripMenuItem.Enabled = false;
                закрытьToolStripMenuItem.Enabled = false;
                if (Fr2 == null) посмотретьToolStripMenuItem.Enabled = true;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                textBox4.Enabled = false;
                textBox5.Enabled = false;
                textBox6.Enabled = false;
                textBox7.Enabled = false;
                textBox9.Enabled = false;
                textBox10.Enabled = false;
                textBox11.Enabled = false;

                // Переменная-индикатор показывает, что последние данные не были сохранены. 
                // Поэтому, при последующем выходе из программы будет показан MessageBox с вопросом:  сохранить данные ?            
                SaveModel = true;

                // Запуск движения:
                timer1.Enabled = true;
            }
            catch (Exception Exp)   //  Обработка исключительной ситуации
            {
                //   Вывод соответствующего сообщения, если произошла исключительная ситуация
                MessageBox.Show($"Некорректный ввод данных. {Exp.Message}\nПовторите попытку.", "ВНИМАНИЕ !!!", MessageBoxButtons.OK);

                //   В переменной Ntb содержится номер textBox-а, в котором произошла ошибка. Так мы узнаем этот textBox и наведем на него фокус ввода, а также выделим имеющийся в нем текст
                switch (Ntb)
                {
                    case 1:    //  ошибка в textBox1
                        textBox1.Focus(); textBox1.SelectAll();
                        break;
                    case 2:    //  ошибка в textBox2
                        textBox2.Focus(); textBox2.SelectAll();
                        break;
                    case 3:    //  ошибка в textBox3
                        textBox3.Focus(); textBox3.SelectAll();
                        break;
                    case 4:    //  ошибка в textBox4
                        textBox4.Focus(); textBox4.SelectAll();
                        break;
                    case 5:    //  ошибка в textBox5
                        textBox5.Focus(); textBox5.SelectAll();
                        break;
                    case 6:    //  ошибка в textBox6
                        textBox6.Focus(); textBox6.SelectAll();
                        break;
                    case 7:    //  ошибка в textBox7
                        textBox7.Focus(); textBox7.SelectAll();
                        break;
                    case 9:    //  ошибка в textBox9
                        textBox8.Focus(); textBox9.SelectAll();
                        break;
                    case 10:   //  ошибка в textBox10
                        textBox10.Focus(); textBox10.SelectAll();
                        break;
                    case 11:   //  ошибка в textBox11
                        textBox11.Focus(); textBox11.SelectAll();
                        break;
                }
            }
            
        }



        //  Процедура выполняется при каждом нажатии кнопки СТОП (button2)
        private void button2_Click(object sender, EventArgs e)    
        {
            // Остановка движения:
            timer1.Enabled = false;

            // Блокировка и разблокировка кнопок:
            button1.Enabled = true;
            button1.Text = "ПРОДОЛЖИТЬ";
            button2.Enabled = false;
            textBox7.Enabled = true;
            сохранитьToolStripMenuItem.Enabled = true;
            закрытьToolStripMenuItem.Enabled = true;
            //посмотретьToolStripMenuItem.Enabled = true;
        }



        //  Процедура выполняется при каждом нажатии кнопки ВЫХОД (button3)
        private void button3_Click(object sender, EventArgs e)    
        {
            Application.Exit();    //   завершить работу приложения
        }



        //  Процедура выполняется при каждом нажатии кнопки Открыть... (в меню)
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)  
        {
            openFileDialog1.ShowDialog();  // развернуть диалоговое окно для ввода имени файла
        }

     



        // Процедура выполняется только, если выбрано имя файла
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)  
        {
            f = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);  // Создание объекта для чтения данных из файла. Имя файла находится в поле openFileDialog1.FileName
            DataOut = new BinaryReader(f);  // Создание более удобного объекта для чтения данных из файла.

            // Чтение данных из файла и запись их в соответствующие переменные.  ВНИМАНИЕ !!!  Данные должны считываться из файла в той же последовательности, в которой они были записаны туда.
            N = DataOut.ReadInt32();
            R = DataOut.ReadDouble();
            Vmax = DataOut.ReadDouble();
            Xmax = DataOut.ReadDouble();
            Ymax = DataOut.ReadDouble();
            dt = DataOut.ReadDouble();
            K = DataOut.ReadDouble();
            Ekin0 = DataOut.ReadDouble();
            t = DataOut.ReadInt32();

            //  Устанавливается значение допустимой погрешности при расчете суммарной кинетической энергии системы
            eps = Ekin0 * 1E-12;   

            // Создание массивов, размеры которых равны количеству чатиц N:
            X = new double[N];
            Y = new double[N];
            Vx = new double[N];
            Vy = new double[N];
            CS = new double[N];
            CN = new int[N];

            // Продолжение чтения данных из файла (заполняются созданные массивы X, Y, Vx, Vy, CS и CN):
            for (int i = 0; i < N; i++)
            {
                X[i] = DataOut.ReadDouble();
                Y[i] = DataOut.ReadDouble();
                Vx[i] = DataOut.ReadDouble();
                Vy[i] = DataOut.ReadDouble();
                CS[i] = DataOut.ReadDouble();
                CN[i] = DataOut.ReadInt32();
            }

            dV = DataOut.ReadDouble();
            NV = DataOut.ReadInt32();
            TimeV = DataOut.ReadInt32();

            // Создание массива счетчиков, размер которого равен количеству интервалов NV в распределении Максвелла:
            CV = new int[NV];

            // Продолжение чтения данных из файла (заполняется созданный массив CV):
            for (int i = 0; i < NV; i++)
            {
                CV[i] = DataOut.ReadInt32();
            }

            // Закрытие файла (чтение данных завершено):
            DataOut.Close();

            // Заполнение textBox-ов считанными из файла параметрами (иначе мы не увидим на экране параметры модели):
            textBox1.Text = N.ToString();
            textBox2.Text = R.ToString();
            textBox3.Text = Vmax.ToString();
            textBox4.Text = (2 * Xmax).ToString();
            textBox5.Text = (2 * Ymax).ToString();
            textBox6.Text = string.Format("{0:f6}", dt);
            textBox7.Text = K.ToString();
            textBox8.Text = string.Format("{0:f6}", t * dt);
            textBox9.Text = dV.ToString();
            textBox10.Text = NV.ToString();
            textBox11.Text = TimeV.ToString();
            textBox12.Text = string.Format("{0:f8}", Ekin0);

            // Нарисовать рисунок:
            DrawPicture();


            // Блокировка и разблокировка кнопок:
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            textBox5.Enabled = false;
            textBox6.Enabled = false;            
            textBox9.Enabled = false;
            textBox10.Enabled = false;
            textBox11.Enabled = false;
            button1.Enabled = true;
            button1.Text = "ПРОДОЛЖИТЬ";
            button2.Enabled = false;
            открытьToolStripMenuItem.Enabled = false;
            сохранитьToolStripMenuItem.Enabled = true;
            закрытьToolStripMenuItem.Enabled = true;
            посмотретьToolStripMenuItem.Enabled = true;

            // Переменная-индикатор, показывающая было ли сохранение последнего состояния модели в файл
            SaveModel = false;

            //  Важный момент !!!  Чтобы модель не считалась новой, переменную-индикатор NewModel следует установить в false
            NewModel = false;  
        }

        

        //  Процедура выполняется при каждом нажатии кнопки Сохранить... (в меню)
        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)  
        {
            saveFileDialog1.ShowDialog();  // развернуть диалоговое окно для ввода имени файла
        }
        
        // Процедура выполняется только, если выбрано имя файла
        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)  
        {
            f = new FileStream(saveFileDialog1.FileName, FileMode.Create, FileAccess.Write);  // Создание объекта для записи данных в файла. Имя файла находится в поле saveFileDialog1.FileName
            DataIn = new BinaryWriter(f);  // Создание более удобного объекта для записи данных в файл.

            // Запись данных в файл из соответствующих переменных и массивов (выполняется в порядке следования команд):
            DataIn.Write(N);
            DataIn.Write(R);
            DataIn.Write(Vmax);
            DataIn.Write(Xmax);
            DataIn.Write(Ymax);
            DataIn.Write(dt);
            DataIn.Write(K);
            DataIn.Write(Ekin0);
            DataIn.Write(t);

            for (int i = 0; i < N; i++)
            {
                DataIn.Write(X[i]);
                DataIn.Write(Y[i]);
                DataIn.Write(Vx[i]);
                DataIn.Write(Vy[i]);
                DataIn.Write(CS[i]);
                DataIn.Write(CN[i]);
            }

            DataIn.Write(dV);
            DataIn.Write(NV);
            DataIn.Write(TimeV);

            for (int i = 0; i < NV; i++)
            {
                DataIn.Write(CV[i]);
            }


            // Закрытие файла (запись данных завершена):
            DataIn.Close();

            // Переменная-индикатор показывает, что данные были только что сохранены. 
            // Поэтому, если теперь осуществить выход из программы или закрыть модель, то не будет показан MessageBox с вопросом:  сохранить данные ?
            SaveModel = false;
        }



        //  Процедура выполняется при каждом нажатии кнопки Закрыть (в меню)
        private void закрытьToolStripMenuItem_Click(object sender, EventArgs e)  
        {
            // Если последние данные не были сохранены (SaveModel == true), то всплывет MessageBox с вопросом:  сохранить данные ?
            if (SaveModel && (MessageBox.Show("Сохранить данные ?", "ВНИМАНИЕ !!!  Данные могут быть потеряны", MessageBoxButtons.OKCancel) == DialogResult.OK))
            {
                saveFileDialog1.ShowDialog();  // развернуть диалоговое окно для ввода имени файла
            }

            // Переменная-индикатор, показывающая было ли сохранение последнего состояния модели в файл
            SaveModel = false;

            // Текущая модель закрыта. Чтобы следующая модель считалась новой, переменную-индикатор NewModel следует установить в true
            NewModel = true;

            // Если была форма StatisticsForm, то  ее уничтожить (на всякий случай)
            if (Fr2 != null)
            {
                Fr2.Visible = false;
                Fr2 = null;
            }

            // Блокировка и разблокировка кнопок:
            button1.Text = "П У С К";
            button1.Enabled = true;
            button2.Enabled = false;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            textBox3.Enabled = true;
            textBox4.Enabled = true;
            textBox5.Enabled = true;
            textBox6.Enabled = true;
            textBox7.Enabled = true;            
            textBox8.Text = "";
            textBox9.Enabled = true;
            textBox10.Enabled = true;
            textBox11.Enabled = true;
            textBox12.Text = "";
            textBox13.Text = "";
            открытьToolStripMenuItem.Enabled = true;
            сохранитьToolStripMenuItem.Enabled = false;
            закрытьToolStripMenuItem.Enabled = false;
            посмотретьToolStripMenuItem.Enabled = false;

            // Стереть рисунок:
            Gr.Clear(pictureBox1.BackColor);
            pictureBox1.Image = BM;
        }



        //  Процедура выполняется при каждом нажатии кнопки Выход (в меню)
        private void выходИзПрограммыToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            Application.Exit();   //   завершить работу приложения
        }



        // Процедура выполняется, если включен timer1 с указанным интервалом в поле Interval
        private void timer1_Tick(object sender, EventArgs e)   
        {
            t++;   // изменение счетчика времени (количества шагов)
            textBox8.Text = string.Format("{0:f6}", t * dt);

            // Движение частиц и обработка их соударения со стенками области:
            for (int i = 0; i < N; i++)
            {
                X[i] += Vx[i] * dt;  // изменение координаты X частиц
                Y[i] += Vy[i] * dt;  // изменение координаты Y частиц
                CS[i] += Math.Sqrt(Vx[i] * Vx[i] + Vy[i] * Vy[i]) * dt;    // изменение счетчика пройденного пути i-й частицы
                if ((X[i] <= -Xmax + R) && (Vx[i] < 0)) Vx[i] = -Vx[i];    // обработка соударения с левой стенкой
                if ((X[i] >= Xmax - R) && (Vx[i] > 0)) Vx[i] = -Vx[i];     // обработка соударения с правой стенкой
                if ((Y[i] <= -Ymax + R) && (Vy[i] < 0)) Vy[i] = -Vy[i];    // обработка соударения с нижней стенкой
                if ((Y[i] >= Ymax - R) && (Vy[i] > 0)) Vy[i] = -Vy[i];     // обработка соударения с верхней стенкой
            }

            // Обработка соударения частиц между собой.  Это самое сложное в проекте !!!
            for (int i = 0; i < N - 1; i++)     // двойной цикл, в котором проверяется на соударение каждая пара частиц
                for (int j = i + 1; j < N; j++) // i и j - это номера рассматриваемых двух частиц 
                {

                    Px = X[j] - X[i];   // первая координата вектора P, соединяющего центры рассматриваемых двух частиц
                    Py = Y[j] - Y[i];   // вторая координата вектора P, соединяющего центры рассматриваемых двух частиц
                    P2 = Px * Px + Py * Py;  // квадрат модуля вектора P, соединяющего центры рассматриваемых двух частиц

                    // Проверка частиц на столкновение:
                    if (P2 <= 4 * R * R)   // если расстояние между центрами рассматриваемых двух чатиц меньше или равно их диаметра, то ...
                                           // это первое и главное условие столкновения частиц 
                    {
                        //  вычисление координат параллельных составляющих скоростей двух рассматриваемых частиц (параллельных по отношению к вектору P)
                        VPix = (Vx[i] * Px + Vy[i] * Py) * Px / P2;
                        VPiy = (Vx[i] * Px + Vy[i] * Py) * Py / P2;
                        VPjx = (Vx[j] * Px + Vy[j] * Py) * Px / P2;
                        VPjy = (Vx[j] * Px + Vy[j] * Py) * Py / P2;

                        if ((VPjx - VPix) * Px + (VPjy - VPiy) * Py < 0)  // здесь проверяется второе (дополнительное) условие столкновения частиц
                                                                          // если частицы сближаются друг к другу, то тогда будет столкновение и нужно изменить их скорости
                        {
                            //  вычисление координат нормальных составляющих скоростей двух столкнувшихся частиц (нормальных по отношению к вектору P)
                            VNix = Vx[i] - VPix;
                            VNiy = Vy[i] - VPiy;
                            VNjx = Vx[j] - VPjx;
                            VNjy = Vy[j] - VPjy;

                            // Тут ВАЖНЫЙ момент !!!  Изменение скоростей двух столкнувшихся частиц.
                            // Столкнувшиеся частицы меняются друг с другом только параллельными состовляющими скоростей, нормальные составляющие скоростей у них остаются прежними.
                            Vx[i] = VPjx + VNix;
                            Vy[i] = VPjy + VNiy;
                            Vx[j] = VPix + VNjx;
                            Vy[j] = VPiy + VNjy;

                            CN[i]++;   // изменение счетчика количества столкновений i-й частицы
                            CN[j]++;   // изменение счетчика количества столкновений j-й частицы
                        }

                    }

                }


            // Рисунок:
            DrawPicture();

            //  Добавление частиц в распределение Максвелла каждые TimeV шагов:
            if (t % TimeV == 0)
            {

                //  Добавление частиц в распределение Максвелла.  Вычисляется index - это номер интервала, в который попала частица в зависимости от ее скорости. Счетчик CV этого интервала увеличивается на 1.
                for (int i = 0; i < N; i++)
                {
                    int index = (int)(Math.Sqrt(Vx[i] * Vx[i] + Vy[i] * Vy[i]) / dV);  //  номер интервала, в который попала i-ая частица
                    if (index < NV) CV[index]++;                                       //  увеличить на 1 счетчик интервала, в который только что попала частица
                }

                //  Расчет средних значений скорости, длины свободного пробега, частоты соударений и среднего квадрата скорости,
                //  а также текущего значения удельной кинетической энергии системы:
                Vsr = 0;
                Lsr = 0;
                Nsr = 0;
                V2sr = 0;
                for (int i = 0; i < N; i++)
                {
                    Vsr += CS[i];
                    Lsr += CS[i] / (CN[i] + 1);
                    Nsr += CN[i];
                    V2sr += Vx[i] * Vx[i] + Vy[i] * Vy[i];
                }
                Ekin = V2sr / 2;            //  текущее значение удельной кинетической энергии системы
                textBox13.Text = string.Format("{0:f8}", Ekin);
                Vsr = Vsr / (t * dt * N);   //  средняя путевая скорость частиц
                Lsr = Lsr / N;              //  средняя длина свободного пробега частиц
                Nsr = Nsr / (t * dt * N);   //  средняя частота соударения частиц между собой
                V2sr = V2sr / N;            //  средний квадрат скорости частиц

                if (Fr2 != null)    //   Если вторая форма в этот момент существует, то ...
                {
                    //  Нарисовать диаграмму. Обращение к методу DrawPicture() производится через Fr2.
                    Fr2.DrawPicture();

                    //  Вывод средних значений в соответствующие textBox-ы, расположенные на StatisticsForm. Поэтому, обращение к ним через Fr2.
                    Fr2.textBox1.Text = Vsr.ToString();
                    Fr2.textBox2.Text = Lsr.ToString();
                    Fr2.textBox3.Text = Nsr.ToString();
                    Fr2.textBox4.Text = V2sr.ToString();

                    //  Заполнение таблицы (объекта dataGridView1). Обращение к dataGridView1 производится через Fr2.
                    Fr2.dataGridView1.ColumnCount = 2;
                    Fr2.dataGridView1.Columns[0].HeaderText = "Интервал";
                    Fr2.dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
                    Fr2.dataGridView1.Columns[1].HeaderText = "Количество частиц";
                    Fr2.dataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
                    Fr2.dataGridView1.RowCount = NV;
                    for (int i = 0; i < NV; i++)
                    {
                        Fr2.dataGridView1[0, i].Value = string.Format("[ {0:f2}, {1:f2})", i * dV, (i + 1) * dV);
                        Fr2.dataGridView1[1, i].Value = string.Format("{0:d}", CV[i]);
                    }

                    //  Обновить текст метки label6 на второй форме
                    Fr2.label6.Text = "";
                }

                if (Math.Abs(Ekin - Ekin0) > eps)  //  Если суммарная кинетическая энергия системы сильно отличается от своего первоначального значения (более, чем на допустимую погрешность eps),
                {                                    //  то это означает, что произошла какая-то техническая ошибка при вычислениях. Тогда нужно сделать следующее:
                    timer1.Enabled = false;          //  остановить движение (выключить timer1)   
                    button2.Enabled = false;                        //  заблокировать соответствующие кнопки
                    открытьToolStripMenuItem.Enabled = false;
                    сохранитьToolStripMenuItem.Enabled = false;
                    закрытьToolStripMenuItem.Enabled = true;
                    SaveModel = false;
                    MessageBox.Show("Произошла серьезная ошибка в расчетах.", "ВНИМАНИЕ !!!", MessageBoxButtons.OK);   //  вывести сообщение
                }
                else   //   Если все в порядке, то произвести автосохранение результатов в специальный файл "temporary.moig"
                {
                    f = new FileStream("temporary.moig", FileMode.Create, FileAccess.Write);  // Создание объекта для записи данных в файла. Имя файла находится в поле saveFileDialog1.FileName
                    DataIn = new BinaryWriter(f);  // Создание более удобного объекта для записи данных в файл.

                    // Запись данных в файл из соответствующих переменных и массивов (выполняется в порядке следования команд):
                    DataIn.Write(N);
                    DataIn.Write(R);
                    DataIn.Write(Vmax);
                    DataIn.Write(Xmax);
                    DataIn.Write(Ymax);
                    DataIn.Write(dt);
                    DataIn.Write(K);
                    DataIn.Write(Ekin0);
                    DataIn.Write(t);

                    for (int i = 0; i < N; i++)
                    {
                        DataIn.Write(X[i]);
                        DataIn.Write(Y[i]);
                        DataIn.Write(Vx[i]);
                        DataIn.Write(Vy[i]);
                        DataIn.Write(CS[i]);
                        DataIn.Write(CN[i]);
                    }

                    DataIn.Write(dV);
                    DataIn.Write(NV);
                    DataIn.Write(TimeV);

                    for (int i = 0; i < NV; i++)
                    {
                        DataIn.Write(CV[i]);
                    }


                    // Закрытие файла (запись данных завершена):
                    DataIn.Close();


                }
            }

        }



        // Процедура выполняется при закрытии главной формы MainForm (т.е. при завершении работы программы)
        private void Form1_FormClosed(object sender, FormClosedEventArgs e) 
        {
            // Если последние данные не были сохранены (SaveModel == true), то всплывет MessageBox с вопросом:  сохранить данные ?
            if (SaveModel && MessageBox.Show("Сохранить данные ?", "ВНИМАНИЕ !!!  Данные могут быть потеряны", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                button2_Click(sender, e);      // вызов процедуры-обработчика нажатия кнопки СТОП (button2)
                saveFileDialog1.ShowDialog();  // развернуть диалоговое окно для ввода имени файла
            }
        }



        //  Процедура выполняется при каждом нажатии кнопки Посмотреть (в меню)
        private void посмотретьToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            //  Расчет средних значений:
            Vsr = 0;
            Lsr = 0;
            Nsr = 0;
            V2sr = 0;
            for (int i = 0; i < N; i++)
            {
                Vsr += CS[i];
                Lsr += CS[i] / (CN[i] + 1);
                Nsr += CN[i];
                V2sr += Vx[i] * Vx[i] + Vy[i] * Vy[i];
            }
            Vsr = Vsr / (t * dt * N);   //  средняя путевая скорость частиц
            Lsr = Lsr / N;              //  средняя длина свободного пробега частиц
            Nsr = Nsr / (t * dt * N);   //  средняя частота соударения частиц между собой
            V2sr = V2sr / N;

            //  Создать и показать вторую (дополнительную) форму StatisticsForm:
            Fr2 = new StatisticsForm();
            Fr2.Visible = true;
            посмотретьToolStripMenuItem.Enabled = false;    //  Заблокировать кнопку "Посмотреть" в меню
            Fr2.DrawPicture();   //  Нарисовать рисунок (гистограмму и график) на pictureBox1 второй формы

            //  Вывод средних значений в соответствующие textBox-ы, расположенные на StatisticsForm. Поэтому, обращение к ним через Fr2.
            Fr2.textBox1.Text = Vsr.ToString();
            Fr2.textBox2.Text = Lsr.ToString();
            Fr2.textBox3.Text = Nsr.ToString();
            Fr2.textBox4.Text = V2sr.ToString();

            //  Заполнение таблицы (объекта dataGridView1). Обращение к dataGridView1 производится через Fr2.
            Fr2.dataGridView1.ColumnCount = 2;
            Fr2.dataGridView1.Columns[0].HeaderText = "Интервал";
            Fr2.dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            Fr2.dataGridView1.Columns[1].HeaderText = "Количество частиц";
            Fr2.dataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
            Fr2.dataGridView1.RowCount = NV;
            for (int i = 0; i < NV; i++)
            {
                Fr2.dataGridView1[0, i].Value = string.Format("[ {0:f2}, {1:f2})", i * dV, (i + 1) * dV);
                Fr2.dataGridView1[1, i].Value = string.Format("{0:d}", CV[i]);
            }

            


        }


    }
}



