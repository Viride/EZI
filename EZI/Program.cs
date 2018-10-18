using EZI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EZI
{
    class Program
    {
        public static Logic logic = new Logic();

        static void Main(string[] args)
        {
            var documents = new List<Document>();
            var stemmedDocuments = new List<StemmedDocument>();
            var keywords = new List<Keyword>();
            var bagOfWords = new BagOfWords();

            string currentDirectory = Directory.GetCurrentDirectory();
            string documentFilePath = System.IO.Path.Combine(currentDirectory, "Texts", "documents.txt");
            string keywordsFilePath = System.IO.Path.Combine(currentDirectory, "Texts", "keywords.txt");

            if (args.Length == 2)
            {
                documents = GetDocuments(args[0]);
                stemmedDocuments = logic.StemDocuments(documents);
                keywords = GetKeywords(args[1]);
            }
            else
            {
                documents = GetDocuments(documentFilePath);
                stemmedDocuments = logic.StemDocuments(documents);
                keywords = GetKeywords(keywordsFilePath);
            }
            //PrintKeywords(keywords);
            keywords = logic.GenerateIdf(stemmedDocuments, keywords);
            bagOfWords = logic.GenerateBagOfWords(stemmedDocuments, keywords);

            var loop = true;
            while (loop)
            {
                Console.WriteLine("__________________________________________________________________________________");
                Console.WriteLine("Wybierz opcję");
                Console.WriteLine("Wcisnij '1' aby wyswietlic dokumenty");
                Console.WriteLine("Wcisnij '2' aby wyswietlic stemmowane keywordsy");
                Console.WriteLine("Wcisnij '3' aby wyszukać");
                Console.WriteLine("Wcisnij '4' aby wyjść");
                Console.WriteLine("__________________________________________________________________________________");
                Console.WriteLine();
                char key = Console.ReadKey().KeyChar;
                Console.WriteLine();
                int id = 0;
                switch (key)
                {
                    case '1':
                        Console.WriteLine("Podaj numer dokumentu lub wciśnij eneter, aby wyświelić wszystkie");
                        Console.WriteLine();
                        var line = Console.ReadLine();
                        if (String.IsNullOrEmpty(line))
                        {
                            PrintDocuments(documents);
                        }
                        else
                        {
                            var nmb = -1;
                            Int32.TryParse(line, out nmb);
                            PrintDocuments(documents, nmb);
                        }
                        break;
                    case '2':
                        PrintKeywords(keywords);
                        break;
                    case '3':
                        Console.WriteLine("Wpisz wyszukiwane słowa");
                        var searchText = Console.ReadLine();
                        var found = logic.Search(searchText, stemmedDocuments, keywords, bagOfWords);
                        PrintSearchResult(found, documents);
                        break;
                    case '4':
                        loop = false;
                        break;
                    default:
                        break;

                }
            }
            //Console.WriteLine("Naciśnij klawisz, aby wyjść");
            //Console.ReadKey();
        }

        public static List<Document> GetDocuments(string documentPath)
        {
            var documents = new List<Document>();
            var Id = 0;
            var file = new System.IO.StreamReader(documentPath);
            string line;
            string contents = "";
            var doc = new Document();
            int lineState = 0;
            while ((line = file.ReadLine()) != null)
            {
                if (lineState == 0)
                {

                    doc.Title = line;
                    lineState = 1;
                    continue;
                }
                if (line == "")
                {
                    doc.Contents = contents;
                    doc.Id = Id;
                    documents.Add(doc);
                    doc = new Document();
                    lineState = 0;
                    contents = "";
                    Id++;
                }
                else
                {
                    contents = contents + line + " ";
                }
            }
            file.Close();


            return documents;
        }

        public static List<Keyword> GetKeywords(string keywordPath)
        {
            var Id = 0;
            var keywords = new List<Keyword>();
            var keys = new List<string>();
            string[] lines = System.IO.File.ReadAllLines(keywordPath);
            foreach (var line in lines)
            {
                var key = logic.StemText(line);
                if (!keys.Contains(key))
                {
                    keywords.Add(new Keyword { key = key, Id = Id });
                    keys.Add(logic.StemText(line));
                    Id++;
                }
            }

            return keywords.Distinct().ToList();
        }

        public static void PrintDocuments(List<Document> documents)
        {
            Console.WriteLine("Documents: ");

            foreach (var doc in documents)
            {
                Console.WriteLine(doc.Id);
                Console.WriteLine(doc.Title);
                Console.WriteLine(doc.Contents);
                Console.WriteLine();
            }
        }

        public static void PrintDocuments(List<Document> documents, int Id)
        {
            if (Id > -1)
            {
                var doc = documents[Id];
                Console.WriteLine(doc.Id);
                Console.WriteLine(doc.Title);
                Console.WriteLine(doc.Contents);
                Console.WriteLine();
            }
        }

        public static void PrintStemmedDocuments(List<StemmedDocument> documents)
        {
            Console.WriteLine("Documents: ");

            foreach (var doc in documents)
            {
                Console.WriteLine(doc.Id);
                Console.WriteLine(logic.ListToString(doc.Title));
                Console.WriteLine(logic.ListToString(doc.Contents));
                Console.WriteLine();
            }
        }

        public static void PrintStemmedDocuments(List<StemmedDocument> documents, int Id)
        {
            var doc = documents[0];
            Console.WriteLine("Not implemented yet");
            Console.WriteLine(doc.Id);
            Console.WriteLine(logic.ListToString(doc.Title));
            Console.WriteLine(logic.ListToString(doc.Contents));
            Console.WriteLine();
        }

        public static void PrintKeywords(List<Keyword> keywords)
        {
            Console.WriteLine("Keys:");
            foreach (var key in keywords)
            {
                Console.WriteLine($"{key.Id}: {key.key}");
            }
            Console.WriteLine();
        }

        public static void PrintSearchResult(IOrderedEnumerable<KeyValuePair<int, double>> result, List<Document> documents)
        {
            Console.WriteLine("Wynik:");
            if (result != null)
            {
                foreach (var res in result)
                {
                    if (res.Value > 0)
                    {
                        Console.WriteLine($"Id: {res.Key}\tTitle: {documents.Single(x => x.Id == res.Key).Title} -> {res.Value}");
                    }
                }
            }
            else Console.WriteLine("Brak wyników");
            Console.WriteLine();
        }
    }
}
