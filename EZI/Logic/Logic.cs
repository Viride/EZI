using EZI.Model;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EZI
{
    public class Logic
    {
        public PorterStemmer stemmer = new PorterStemmer();

        public string StemText(string text)
        {
            return stemmer.stemTerm(text);
        }

        public List<StemmedDocument> StemDocuments(List<Document> documents)
        {
            var stemmedDocuments = new List<StemmedDocument>();
            foreach (var document in documents)
            {
                var stemmedDoc = new StemmedDocument();
                stemmedDoc.Id = document.Id;
                stemmedDoc.Title = PreProcessDocument(document.Title);
                stemmedDoc.Contents = PreProcessDocument(document.Contents);
                stemmedDocuments.Add(stemmedDoc);
            }

            return stemmedDocuments;
        }

        public List<string> PreProcessDocument(string text)
        {
            var newText = text.ToLower();
            //var trimedText = TrimPunctuation(newText);
            var stemmedText = new List<string>();
            string[] separators = new string[] { ",", ".", "!", "\'", " ", "\'s", "?", ":", ";", "\"", "|", "\\", "/" };
            foreach (var word in newText.Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                stemmedText.Add(StemText(word));
            }
            return stemmedText;
        }

        public List<Keyword> GenerateIdf(List<StemmedDocument> documents, List<Keyword> keywords)
        {
            double count = documents.Count;

            foreach (var document in documents)
            {
                var words = document.Title.Concat(document.Contents).ToList();
                foreach (var key in keywords)
                {
                    if (words.Contains(key.key))
                    {
                        key.DocWithKeyCount += 1;
                    }
                }
            }
            foreach (var key in keywords)
            {
                if (key.DocWithKeyCount > 0) key.Idf = Math.Log10((count / (double)key.DocWithKeyCount));
                else key.Idf = 0;
            }
            return keywords;
        }

        public BagOfWords GenerateBagOfWords(List<StemmedDocument> documents, List<Keyword> keywords)
        {
            var bag = new BagOfWords();
            bag.BagOfWord = new Dictionary<int, Dictionary<string, double>>();
            bag.Vectors = new Dictionary<int, double>();
            //(liczba wystąpień / maksymalną liczbę wystąpień) * idf
            foreach (var document in documents)
            {
                var documentBag = new Dictionary<string, double>();
                foreach (var key in keywords)
                {
                    var count = document.Title.Concat(document.Contents).Count(x => x == key.key);
                    documentBag.Add(key.key, count);
                }
                var max = documentBag.Select(x => x.Value).Max();
                if (max != 0)
                {
                    double value = 0;
                    for (int index = 0; index < documentBag.Count; index++)
                    {
                        var dict = documentBag.ElementAt(index);
                        double val = (dict.Value / max) * keywords.SingleOrDefault(x => x.key == dict.Key).Idf;
                        documentBag[dict.Key] = val;
                        value = value + Math.Pow(val, 2);
                    }
                    bag.BagOfWord.Add(document.Id, documentBag);
                    bag.Vectors.Add(document.Id, Math.Sqrt(value));
                }
            }
            return bag;
        }

        public IOrderedEnumerable<KeyValuePair<int, double>> Search(string text, List<StemmedDocument> documents, List<Keyword> keywords, BagOfWords bagOfWords)
        {
            var result = new Dictionary<int, double>();
            var lowerText = text.ToLower();
            var words = StringToListOfString(lowerText);
            var stemmed = new List<string>();
            foreach (var word in words)
            {
                stemmed.Add(StemText(word));        //stemmowanie zapytania
            }

            var Q = new Dictionary<string, double>();
            foreach (var key in keywords)
            {
                var count = stemmed.Count(x => x == key.key);       //zliczanie wystąpień w zapytaniu
                Q.Add(key.key, count);
            }
            var max = Q.Select(x => x.Value).Max();
            if (max == 0)
            {
                return null;
            }
            double value = 0;
            for (int index = 0; index < Q.Count; index++)           //obliczanie bag of words w tf-idf
            {
                var dict = Q.ElementAt(index);
                double val = (dict.Value / max) * keywords.SingleOrDefault(x => x.key == dict.Key).Idf;
                Q[dict.Key] = val;
                value = value + Math.Pow(val, 2);
            }
            var QVector = Math.Sqrt(value);             //|Q|
            foreach (var bag in bagOfWords.BagOfWord)
            {
                double val = 0;
                foreach (var key in keywords)
                {
                    val = val + Q[key.key] * bag.Value[key.key];
                }
                result.Add(bag.Key, val / (QVector * bagOfWords.Vectors[bag.Key]));     //sim(Q, Dx)
            }
            var sortedDict = from entry in result orderby entry.Value descending select entry;      //sortowanie wyniku
            //return result;
            return sortedDict;
        }

        public Matrix<double> GenerateSimilarityKeywords(List<StemmedDocument> documents, List<Keyword> keywords)
        {
            var keywordsCount = new Dictionary<int, Dictionary<string, double>>();
            var similarityKeywords = new Dictionary<int, Dictionary<string, double>>();
            var matrix = Matrix<double>.Build.Dense(keywords.Count, documents.Count);
            foreach (var document in documents)
            {
                foreach (var key in keywords)
                {
                    matrix[key.Id, document.Id] = document.Title.Concat(document.Contents).Count(x => x == key.key);
                }
            }

            for (int i = 0; i < keywords.Count; i++)
            {
                double val = 0;
                for (int j = 0; j < documents.Count; j++)
                {
                    val = val + Math.Pow(matrix[i, j], 2);
                }
                val = Math.Sqrt(val);
                for (int j = 0; j < documents.Count; j++)
                {
                    matrix[i, j] = matrix[i, j] / val;
                }
            }

            var matrixT = matrix.Transpose();
            return matrix * matrixT;
        }

        public IOrderedEnumerable<KeyValuePair<string, double>> SearchExtended(string text, Matrix<double> similarity, List<Keyword> keywords)
        {
            var lowerText = text.ToLower();
            var words = StringToListOfString(lowerText);
            var stemmed = new List<string>();
            foreach (var word in words)
            {
                stemmed.Add(StemText(word));        //stemmowanie zapytania
            }
            if (stemmed.Count == 1)
            {
                if (keywords.Select(x => x.key).Contains(stemmed[0]))
                {
                    var result = new Dictionary<string, double>();
                    var row = keywords.Single(x => x.key == stemmed[0]).Id;
                    foreach (var key in keywords)
                    {
                        if (row != key.Id)
                        {
                            var str = "" + stemmed[0] + " " + key.key;
                            result.Add(str, similarity[row, key.Id]);
                        }
                    }
                    var sortedDict = from entry in result orderby entry.Value descending select entry;      //sortowanie wyniku
                    return sortedDict;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var count = 0;
                double[] array = new double[keywords.Count];
                for(int i=0;i<array.Length; i++)
                {
                    array[i] = 0;
                }
                var result = new Dictionary<string, double>();
                foreach (var word in stemmed)
                {
                    if (keywords.Select(x => x.key).Contains(word))
                    {
                        count++;
                        var row = keywords.Single(x => x.key == word).Id;
                        foreach (var key in keywords)
                        {
                            if (row != key.Id)
                            {
                                array[key.Id] += similarity[row, key.Id];
                            }
                        }
                    }
                }
                foreach (var key in keywords)
                {
                    var str = text + " " + key.key;
                    result.Add(str, array[key.Id]/count);
                }
                var sortedDict = from entry in result orderby entry.Value descending select entry;      //sortowanie wyniku
                return sortedDict;
            }
        }

        public string ListToString(List<string> list)
        {
            string text = "";
            foreach (var el in list)
            {
                text = text + el + " ";
            }
            return text;
        }

        public List<string> StringToListOfString(string text)
        {
            string[] separators = new string[] { ",", ".", "!", "\'", " ", "\'s", "?", ":", ";", "\"", "|", "\\", "/" };
            return text.Split(separators, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}