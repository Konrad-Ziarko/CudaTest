﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomExtensions;
using Zniffer.Properties;
using Zniffer.Levenshtein;
using Trinet.Core.IO.Ntfs;

namespace Zniffer.FilesAndText {
    class Searcher {
        private MainWindow window;
        
        public Searcher(MainWindow window) {
            this.window = window;
        }

        public void SearchFiles(List<string> files, DriveInfo drive) {
            foreach (string file in files) {
                //Console.Out.WriteLine(File.ReadAllText(file));
                try {
                    Console.WriteLine("Skanowanie " + file);
                    LevenshteinMatches matches = SearchPhraseInFile(file);
                    Console.WriteLine("Przeskanowano " + file);
                    //foreach(string str in File.ReadLines(file))
                    if (matches.hasMatches) {
                        window.AddTextToFileBox(file);
                        window.AddTextToFileBox("\n");
                        window.AddTextToFileBox(matches);
                    }
                }
                catch (UnauthorizedAccessException) {
                    window.AddTextToFileBox("Cannot access:" + file);
                }
                catch (IOException) {
                    //device detached
                }
                if (Settings.Default.ScanADS && drive.DriveFormat.Equals("NTFS")) {
                    //search for ads
                    //string fileName = Path.GetFileName(file);
                    FileInfo fileInfo = new FileInfo(file);
                    
                    foreach (AlternateDataStreamInfo stream in fileInfo.ListAlternateDataStreams()) {
                        string streamName = stream.Name;
                        AlternateDataStreamInfo s = fileInfo.GetAlternateDataStream(stream.Name, FileMode.Open);
                        LevenshteinMatches matches = null;
                        using (StreamReader reader = s.OpenText()) {
                            matches = ExtractPhrase(reader.ReadToEnd());
                        }
                        if (matches.hasMatches) {
                            window.AddTextToFileBox(file+":"+streamName);
                            window.AddTextToFileBox("\n");
                            window.AddTextToFileBox(matches);
                        }
                    }
                }
            }
        }

        public List<string> GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly) {

            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private List<string> GetDirectories(string path, string searchPattern) {
            try {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (UnauthorizedAccessException) {
                return new List<string>();
            }
        }

        public List<string> GetFiles(string path) {
            string searchPattern = "*";//look for any file
            try {
                return Directory.GetFiles(path, searchPattern).ToList();
            }
            catch (IOException) {
                return new List<string>();
            }
            catch (UnauthorizedAccessException) {
                return new List<string>();
            }
        }


        public LevenshteinMatches ExtractPhrase(string sourceText) {
            string phrase = MainWindow.SearchPhrase;
            LevenshteinMatches matches = sourceText.Levenshtein(phrase, mode: window.scanerMode);
            return matches;
        }

        public LevenshteinMatches SearchPhraseInFile(string filePath) {
            string textFromFile = File.ReadAllText(filePath);
            return ExtractPhrase(textFromFile);
        }

    }
}
