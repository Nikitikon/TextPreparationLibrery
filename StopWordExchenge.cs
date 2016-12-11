using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TextPreparationLibrery
{
    public class FrequencyMatrix
    {
        private List<string> Texts { get; }

        public FrequencyMatrix(List<string> Texts)
        {
            this.Texts = Texts;
        }

        public void CreateFrequencyMatrix(out double[,] UsualMatrix, out double[,] TFIDFMatrix, out int textLenght, out int wordLenght, out long UsualMatrixTime, out long TFIDFMatrixTime, out List<string> NotAloneWorld)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<List<string>> TextsList = new List<List<string>>();
            RusTextPreparation textprep = new RusTextPreparation();
            foreach (string item in Texts)
            {
                Task<List<string>> task = new Task<List<string>>(() => textprep.Preparation(item));
                task.Start();
                TextsList.Add(task.Result);
            }

            Task.WaitAll();

            NotAloneWorld = AloneWord.DeleteAloneWord(TextsList);

            textLenght = TextsList.Count;
            wordLenght = NotAloneWorld.Count;

            long temptime = stopwatch.ElapsedMilliseconds;
            UsualMatrixTime = temptime;
            TFIDFMatrixTime = temptime;
            stopwatch.Restart();


            CreateMatrix cm = new CreateMatrix(TextsList);
            UsualMatrix = cm.CreateFrequencyMatrix(NotAloneWorld);
            UsualMatrixTime += stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            MatrixTFIDF mtfidf = new MatrixTFIDF(TextsList);
            TFIDFMatrix = mtfidf.CreateTFIDFMatrix(NotAloneWorld);
            TFIDFMatrixTime += stopwatch.ElapsedMilliseconds;

            stopwatch.Stop();

        }
    }

    internal class RusTextPreparation
    {
        private static List<string> StopWordList = new List<string>();

        static RusTextPreparation()
        {
            inicList();
        }

        private static void inicList()
        {
            try
            {
                using (StreamReader sr = new StreamReader("TextFile2.txt", System.Text.Encoding.Default))
                {
                    while (!sr.EndOfStream)
                    {
                        StopWordList.Add(sr.ReadLine());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public List<string> Preparation(string str)
        {
            str = DeleteStopSign(str);
            List<string> WordList = incStopList(str);
            if (WordList == null)
                throw new Exception(); // создать класс с ошибкой разбиения на слова
            WordList = DeleteStopWord(WordList);
            WordList = Stream(WordList);
            return WordList;
        }

        private string DeleteStopSign(string str)
        {
            str = Regex.Replace(str, " - ", " ");
            string pattern = "[.?!)(,:\"'@#$%^&*;/\\|{}\\]\\\\[]";
            str = Regex.Replace(str, pattern, "");
            return str;
        }

        private List<string> incStopList(string str)
        {
            List<string> StopList;

            try
            {
                StopList = str.Split(' ').ToList();
                return StopList;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private List<string> DeleteStopWord(List<string> WordList)
        {
            List<string> NewWordList;
            var temp = from item in WordList
                       where !StopWordList.Contains(item.ToLower())
                       select item;
            NewWordList = temp.ToList<string>();

            return NewWordList;
        }

        private List<string> Stream(List<string> list)
        {
            Porter porter = new Porter();
            List<string> newList = new List<string>();
            foreach (string item in list)
            {
                newList.Add(porter.Stemm(item));
            }

            return newList;
        }

    }

    internal class Porter
    {

        private const string VOWEL = "аеиоуыэюя";

        private const string PERFECTIVEGROUND = "((ив|ивши|ившись|ыв|ывши|ывшись)|((?<=[ая])(в|вши|вшись)))$";

        private const string REFLEXIVE = "(с[яь])$";

        private const string ADJECTIVE = "(ее|ие|ые|ое|ими|ыми|ей|ий|ый|ой|ем|им|ым|ом|его|ого|ему|ому|их|ых|ую|юю|ая|яя|ою|ею)$";

        private const string PARTICIPLE = "((ивш|ывш|ующ)|((?<=[ая])(ем|нн|вш|ющ|щ)))$";

        private const string VERB = "((ила|ыла|ена|ейте|уйте|ите|или|ыли|ей|уй|ил|ыл|им|ым|ен|ило|ыло|ено|ят|ует|уют|ит|ыт|ены|ить|ыть|ишь|ую|ю)|((?<=[ая])(ла|на|ете|йте|ли|й|л|ем|н|ло|но|ет|ют|ны|ть|ешь|нно)))$";

        private const string NOUN = "(а|ев|ов|ие|ье|е|иями|ями|ами|еи|ии|и|ией|ей|ой|ий|й|иям|ям|ием|ем|ам|ом|о|у|ах|иях|ях|ы|ь|ию|ью|ю|ия|ья|я)$";

        private const string RVRE = "^(.*?[аеиоуыэюя])(.*)$";

        private const string DERIVATIONAL = ".*[^аеиоуыэюя]+[аеиоуыэюя].*ость?$";

        private const string DER = "ость?$";

        private const string SUPERLATIVE = "(ейше|ейш)$";

        private const string I = "и$";
        private const string P = "ь$";
        private const string NN = "нн$";

        public string Stemm(string word)
        {
            word = word.ToLower();
            word = word.Replace("ё", "е");
            Regex regex = new Regex(RVRE);
            if (regex.IsMatch(word))
            {
                string pre = regex.Match(word).Groups[1].Value;
                string rv = regex.Match(word).Groups[2].Value;
                string temp = Regex.Replace(rv, PERFECTIVEGROUND, "");
                if (temp.Equals(rv))
                {
                    rv = Regex.Replace(rv, REFLEXIVE, "");
                    temp = Regex.Replace(rv, ADJECTIVE, "");
                    if (!temp.Equals(rv))
                    {
                        rv = temp;
                        rv = Regex.Replace(rv, PARTICIPLE, "");
                    }
                    else
                    {
                        temp = Regex.Replace(rv, VERB, "");
                        if (temp.Equals(rv))
                        {
                            rv = Regex.Replace(rv, NOUN, "");
                        }
                        else
                        {
                            rv = temp;
                        }
                    }
                }
                else
                {
                    rv = temp;
                }

                rv = Regex.Replace(rv, I, "");
                if (IsMatch(rv, DERIVATIONAL))
                {
                    rv = Regex.Replace(rv, DER, "");
                }

                temp = Regex.Replace(rv, P, "");
                if (temp.Equals(rv))
                {
                    rv = Regex.Replace(rv, SUPERLATIVE, "");
                    rv = Regex.Replace(rv, NN, "");
                }
                else
                {
                    rv = temp;
                }
                word = pre + rv;
            }

            return word;
        }

        private bool IsMatch(string word, string matchingPattern)
        {
            return new Regex(matchingPattern).IsMatch(word);
        }
    }

    internal class AloneWord
    {
        public static List<string> DeleteAloneWord(List<List<string>> Texts)
        {
            List<string> TempTexts = new List<string>();
            foreach (List<string> list in Texts)
            {
                foreach (string item in list)
                {
                    int count = (from text in Texts
                                 from word in text
                                 where item.Equals(word)
                                 select word).Count();

                    if (count != 1 && !TempTexts.Contains(item))
                    {
                        TempTexts.Add(item);
                    }
                }
            }

            return TempTexts;
        }
    }

    internal class MatrixTFIDF
    {
        private List<List<string>> Texts;
        public MatrixTFIDF(List<List<string>> Texts)
        {
            this.Texts = Texts;
        }

        private double TF(string word, List<string> Text)
        {
            double wordCount = (from item in Text
                                where word.Equals(item)
                                select item).Count();

            return wordCount / (double)Text.Count;
        }

        private double IDF(string word)
        {
            double countWordInText = (from text in Texts
                                      where text.Contains(word)
                                      select text).Count();

            return Math.Log10(Texts.Count / (double)countWordInText);
        }

        private double TFIDF(string word, List<string> text)
        {
            double tf = TF(word, text);
            double idf = IDF(word);
            return (tf * idf);
        }

        public double[,] CreateTFIDFMatrix(List<string> NotAloneWord)
        {
            int textLenght = Texts.Count;
            int wordLenght = NotAloneWord.Count;
            double[,] FrequencyMatrix = new double[wordLenght, textLenght + 1];
            int i = 0;
            int j = 0;

            foreach (List<string> list in Texts)
            {
                foreach (string item in NotAloneWord)
                {
                    FrequencyMatrix[i, j] = TFIDF(item, list);
                    i++;
                }
                j++;
                i = 0;
            }


            return FrequencyMatrix;
        }
    }

    internal class CreateMatrix
    {
        private List<List<string>> Texts;
        public CreateMatrix(List<List<string>> Texts)
        {
            this.Texts = Texts;
        }

        public double[,] CreateFrequencyMatrix(List<string> NotAloneWord)
        {
            int textLenght = Texts.Count;
            int wordLenght = NotAloneWord.Count;
            double[,] FrequencyMatrix = new double[wordLenght, textLenght + 1];
            int i = 0;
            int j = 0;
            foreach (List<string> list in Texts)
            {

                foreach (string word in NotAloneWord)
                {
                    int count = (from item in list
                                 where item.Equals(word)
                                 select item).Count();
                    FrequencyMatrix[i, j] = count;
                    i++;
                }
                j++;
                i = 0;
            }

            return FrequencyMatrix;
        }
    }
}
