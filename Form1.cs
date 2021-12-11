using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ContestPlacesWithoutPoints
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                this.label1.Text = "";
                Application.DoEvents();

                var r = ParseAndcalculate(this.textBox1.Text);
                save(r, this.textBox1.Text + ".result.txt");
                this.label1.Text = "Рассчёт закончен, результат в файле " + this.textBox1.Text + ".result.txt";
            }
            catch (Exception ex)
            {
                this.label1.Text = ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            label1.Text = "";
            if (string.IsNullOrEmpty(openFileDialog1.InitialDirectory))
                openFileDialog1.InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");

            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            this.textBox1.Text = openFileDialog1.FileName;
        }

        class Result
        {
            public int[]      EstimateGroups;
            public List<TextClass> texts;
        }

        class TextClass: IComparable<TextClass>, IComparer<TextClass>
        {
            public int    place;
            public string Name;
            public int[]  estimates;
            public Result r;

            public int max;
            public int min;
            public int summ;

            public int clonedOldNumber;

            public List<int> compared;

            public TextClass(Result r, string Name, string estString, bool isReversed, bool WithoutPriorities = false)
            {
                this.r    = r;
                this.Name = Name;

                var est = estString.Split(new string[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries);

                var len = r.EstimateGroups.Length;
                if (len == 0 && Program.mainForm.WithoutPrioritiesBox.Checked)
                {
                    len = est.Length;
                    r.EstimateGroups = new int[len];
                    for (int i = 0; i < len; i++)
                        r.EstimateGroups[i] = i;
                }
                this.estimates = new int[len];

                for (int i = 0; i < len; i++)
                {
                    this.estimates[i] = Int32.Parse(est[r.EstimateGroups[i]]);
                    if (isReversed)
                        this.estimates[i] *= -1;
                }

                this.WithoutPriorities = WithoutPriorities;
            }

            protected TextClass()
            {
            }

            public TextClass Clone(Result newr, int clonedOldNumber)
            {
                var r  = new TextClass();
                r.r    = newr;
                r.Name = this.Name;
                r.clonedOldNumber = clonedOldNumber;
                r.estimates = (int[]) this.estimates.Clone();
                r.WithoutPriorities = this.WithoutPriorities;

                return r;
            }

            public int maxPlace()
            {
                int result = this.estimates[0];
                for (int i = 1; i < this.estimates.Length; i++)
                {
                    if (result < this.estimates[i])
                        result = this.estimates[i];
                }

                return result;
            }

            /// <summary>Без приоритетов: все судьи (параметры) равны. Конкурсанты равны тогда, когда равно количество "за" и "против" них вне зависимости от их порядка.</summary>
            public bool WithoutPriorities = false;

            // Если объект лучше по большему количеству признаков - то объект побеждает другой объект
            // Если возвращаемый результат меньше нуля - значит this хуже, чем other
            public int CompareTo(TextClass other)
            {
                var cnt =  0;
                for (int i = 0; i < this.estimates.Length; i++)
                {
                    // Если место в группе ниже, то объект выше
                    if (this.estimates[i] < other.estimates[i])
                        cnt++;
                    else
                    if (this.estimates[i] > other.estimates[i])
                        cnt--;
                }

                if (cnt > 0)
                    return 1;
                else
                if (cnt < 0)
                    return -1;

                if (WithoutPriorities)
                    return 0;

                // Сравниваем по приоритетам групп
                for (int i = 0; i < this.estimates.Length; i++)
                {
                    if (this.estimates[i] < other.estimates[i])
                        return 1;
                    else
                    if (this.estimates[i] > other.estimates[i])
                        return -1;
                }

                return 0;
            }

            public void isMax()
            {
                this.max  = 0;
                this.summ = 0;
                this.min  = 0;

                for (int i = 0; i < this.compared.Count; i++)
                {
                    if (this.compared[i] > 0)
                    {
                        max++;
                        summ++;
                    }
                    
                    if (this.compared[i] < 0)
                    {
                        summ--;
                        min++;
                    }
                }
            }

            int IComparer<TextClass>.Compare(TextClass x, TextClass y)
            {
                return x.place - y.place;
            }
        }

        private Result ParseAndcalculate(string FileName)
        {
            var r = new Result();

            var lines = File.ReadAllLines(FileName, /*Encoding.GetEncoding(1251)*/Encoding.UTF8);

            bool isReversed = ReverseBox.Checked;
            getEstimateGroups(r, lines);
            addTexts(r, lines, isReversed);
            calculate(r);

            return r;
        }

        private void calculate(Result r)
        {
            compareTexts(r);
            placesTexts(r);
        }

        private void placesTexts(Result r)
        {
            var txts = new List<TextClass>(r.texts);

            int place = 1;
            while (txts.Count > 0)
            {
                var list = new List<int>(2);
                int cntI = 0;

                for (int i = 0; i < txts.Count; i++)
                    txts[i].isMax();

                for (int i = 0; i < txts.Count; i++)
                {
                    // Если соревнователь никому не уступает
                    if (txts[i].min == 0)
                    {
                        var txt = txts[i];
                        txt.place = place;
                        list.Add(i);

                        cntI++;
                    }
                }

                // Если все уступают хоть кому-нибудь, то есть первое место не очевидно
                // Ищем всех, у кого максимальное количество соревнователей, над которыми они круче
                if (cntI <= 0)
                {
                    int maxSumm = int.MinValue;
                    for (int i = 0; i < txts.Count; i++)
                    {
                        //if (txts[i].summ > maxSumm)
                        if (txts[i].max > maxSumm)
                        {
                            // maxSumm = txts[i].summ;
                            maxSumm = txts[i].max;
                        }
                    }

                    for (int i = 0; i < txts.Count; i++)
                    {
                        //if (txts[i].summ >= maxSumm)
                        if (txts[i].max >= maxSumm)
                        {
                            var txt = txts[i];
                            txt.place = place;
                            list.Add(i);

                            cntI++;
                        }
                    }
                }

                // Из уже выделенных лучших снова создаём отдельный турнир и решаем уже там
                if (list.Count > 1)
                {
                    var newR = new Result();
                    newR.EstimateGroups = (int[]) r.EstimateGroups.Clone();
                    newR.texts          = new List<TextClass>(r.texts.Count);

                    for (int i = 0; i < list.Count; i++)
                    {
                        newR.texts.Add((TextClass) txts[list[i]].Clone(newR, list[i]));
                    }

                    // Если выделено меньше равных произведений, чем есть в исходном оцениваемом списке
                    if (r.texts.Count > list.Count)
                    {
                        calculate(newR);
                        var list2 = new List<int>(list.Count);
                        for (int i = 0; i < newR.texts.Count; i++)
                        {
                            if (newR.texts[i].place == 1)
                            {
                                list2.Add(newR.texts[i].clonedOldNumber);
                            }
                        }

                        list = list2;
                        list2 = null;
                    }
                    // Если разрешить спор между конкурсантами не удалось
                    else
                    {/*
                        // Ищем тех, у кого меньше всего произведений, которым они уступают
                        int minSumm = int.MaxValue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (txts[list[i]].min < minSumm)
                            {
                                minSumm = txts[list[i]].min;
                            }
                        }

                        for (int i = 0; i < list.Count; i++)
                        {
                            if (txts[list[i]].min == minSumm)
                            {
                            }
                            else
                            {
                                list.RemoveAt(i);
                                i--;
                                cntI--;
                            }
                        }*/
                    }
                }

                list.Sort();
                for (int i = list.Count - 1; i >= 0; i--)
                    remove(txts, list[i]);

                if (cntI <= 0)
                    throw new Exception("Не удаётся распределить место " + place);

                place++;
            }
        }

        private static void remove(List<TextClass> txts, int i)
        {
            txts.RemoveAt(i);

            for (int j = 0; j < txts.Count; j++)
            {
                txts[j].compared.RemoveAt(i);
            }
        }

        private void compareTexts(Result r)
        {
            var txts = r.texts;
            for (int i = 0; i < txts.Count; i++)
            {
                txts[i].compared = new List<int>(txts.Count);
                for (int j = 0; j < txts.Count; j++)
                    txts[i].compared.Add(0);
            }

            for (int i = 0; i < txts.Count; i++)
            {
                txts[i].compared[i] = 0;
                for (int j = i + 1; j < txts.Count; j++)
                {
                    txts[i].compared[j] = txts[i].CompareTo(txts[j]);
                    txts[j].compared[i] = -txts[i].compared[j];
                }
            }
        }

        private static void addTexts(Result r, string[] lines, bool isReversed)
        {
            r.texts = new List<TextClass>(lines.Length >> 1);
            for (int i = 1; i < lines.Length; i++)
            {
                var t = lines[i].Trim();
                if (t.Length <= 0 || t.StartsWith("#"))
                    continue;

                
                var txt = new TextClass(r, t, lines[i + 1].Trim(), isReversed, Program.mainForm.WithoutPrioritiesBox.Checked);
                r.texts.Add(txt);
                i++;
            }
        }

        private static void getEstimateGroups(Result r, string[] lines)
        {
            var eg = lines[0].Trim().Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (eg.Length < 1)
            {
                if (Program.mainForm.WithoutPrioritiesBox.Checked)
                {
                    r.EstimateGroups = new int[0];
                    return;
                }
                else
                    throw new Exception("Неверно заполнена первая строка: первая строка - приоритеты груп при оценке. Номера групп начинаются с нуля, левая группа самая сильная. Первая строка вообще не заполнена");
            }

            r.EstimateGroups = new int[eg.Length];
            for (int i = 0; i < eg.Length; i++)
            {
                r.EstimateGroups[i] = Int32.Parse(eg[i].Trim());
            }

            for (int a = 0; a < r.EstimateGroups.Length; a++)
            {
                var fnd = false;
                for (int i = 0; i < r.EstimateGroups.Length; i++)
                {
                    if (r.EstimateGroups[i] == a)
                    {
                        fnd = true;
                        break;
                    }
                }

                if (!fnd)
                    throw new Exception("Неверно заполнена первая строка: первая строка - приоритеты груп при оценке. Номера групп начинаются с нуля, левая группа самая сильная.");
            }
        }
        
        private void save(Result r, string FileName)
        {
            var sb = new StringBuilder();
            
            bool isReversed = ReverseBox.Checked;
            if (isReversed)
                sb.AppendLine("Принцип подсчёта: чем больше параметр - тем лучше");
            else
                sb.AppendLine("Принцип подсчёта: чем меньше параметр (чем выше место) - тем лучше");

            if (WithoutPrioritiesBox.Checked)
                sb.AppendLine("Все судьи равны");
            else
                sb.AppendLine("Приоритеты судей заданы");

            r.texts.Sort(r.texts[0]);
            for (int i = 0; i < r.texts.Count; i++)
            {
                sb.AppendLine(r.texts[i].place.ToString("D2") + ". " + r.texts[i].Name);
            }

            File.WriteAllText(FileName, sb.ToString());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = File.ReadAllText("help.md");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
