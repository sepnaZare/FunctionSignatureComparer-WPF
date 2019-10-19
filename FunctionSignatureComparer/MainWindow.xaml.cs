using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

namespace FunctionSignatureComparer
{
    public partial class MainWindow : Window
    {
        private string GitRepositoryPath { get; set; }
        private string ResultFilePath { get; set; }

        private Regex MutipleLineParameterAdded => new Regex(@"(public|private|protected)\s+(static\s+|\s)?\w+\s+\w+\s*\((.|(\n|\r|\r\n))*?\)(\s|\w)*");
        private Regex InlineMethodDeclarationDeleted => new Regex(@"-(\s)*(public|private|protected)\s+(static\s+|\s)?\w+\s+\w+\s*\(.*?\)(\s|\w)*");
        private Regex InlineMethodDeclarationAdded => new Regex(@"\+(\s)*(public|private|protected)\s+(static\s+|\s)?\w+\s+\w+\s*\((.|(\n|\r|\r\n))*?\)(\s|\w)*");


        public MainWindow()
        {
            InitializeComponent();
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(GitRepositoryPath))
            {
                System.Windows.MessageBox.Show("Select a valid git repository path.");
                return;
            }

            Results.Text += "Processing...";

            CompareFunctionsSignitureBetweenCommits();

            Results.Text += "Completed...";
        }

        private void CompareFunctionsSignitureBetweenCommits()
        {
            var resultFilePath = string.IsNullOrEmpty(ResultFilePath) ? $"{GitRepositoryPath}\\Result.csv" : ResultFilePath;
            StringWriter finalResult = new StringWriter();


            finalResult.WriteLine(string.Format("{0},{1},{2},{3}", "commitSHA", "JavaFile", "oldFunctionSignature", "newFunctionSignature"));
          
            using (var repo = new Repository(GitRepositoryPath))
            {
                foreach (var commit in repo.Head.Commits)
                {
                    var commitTree = commit.Tree;
                    var parentCommitTree = commit?.Parents.FirstOrDefault()?.Tree;

                    //if (commit.Parents.Count() > 1)
                    //    Results.Text += "I have more than one parent";

                    if (parentCommitTree == null)
                        continue;

                    TreeChanges treeChanges = repo.Diff.Compare(parentCommitTree, commitTree);

                    foreach (TreeEntryChanges treeEntryChanges in treeChanges)
                    {
                        var changesInFile = Regex.Split(treeEntryChanges.Patch, @"\n@@");
                        foreach (var fileChangedSection in changesInFile) {
                            var changedPart = InlineMethodDeclarationDeleted.Match(fileChangedSection);
                            if (changedPart.Success)
                            {
                                var result = DetectInlineParameterAdd(commit, fileChangedSection, changedPart, treeEntryChanges.Path);
                                if (!string.IsNullOrEmpty(result))
                                    finalResult.WriteLine(result);
                                continue;
                            }

                            changedPart = MutipleLineParameterAdded.Match(fileChangedSection);
                            if (changedPart.Success)
                            {
                                var result = DetectNewLineParameterAdd(commit, fileChangedSection, changedPart, treeEntryChanges.Path);
                                if (!string.IsNullOrEmpty(result))
                                    finalResult.WriteLine(result);
                                continue;
                            }
                        }

                    }

                }

            }

            File.WriteAllText(resultFilePath, finalResult.ToString(), new UTF8Encoding());
        }

        private string DetectNewLineParameterAdd(Commit commit, string fileChangedSection, Match deletedPart, string javaFile)
        {
            Regex AllChangesDetection = new Regex(@"(public|private|protected)\s+(static\s+|\s)?\w+\s+\w+\s*\((.|(\n|\r|\r\n))*?\) (.|(\n|\r|\r\n))*?\)(\s|\w)*{");
            
            Match signatureAllchanges = AllChangesDetection.Match(fileChangedSection);

            if (!signatureAllchanges.Value.Contains("+ "))
                return string.Empty;
            
            int indexOfMinus = signatureAllchanges.Value.IndexOf('-');
            int indexOfPlus = signatureAllchanges.Value.IndexOf('+', indexOfMinus + 1);
            if(indexOfMinus < 0 || indexOfPlus < 0 || !signatureAllchanges.Success)
                return string.Empty;

            var addedPart = (signatureAllchanges.Value.Substring(0, indexOfMinus) + signatureAllchanges.Value.Substring(indexOfPlus + 1));

            if (!IsChangedParameterCount(addedPart, deletedPart.Value))
                return String.Empty;

            var commitSHA = "\"" + commit.Sha + "\"";
            string newFunctionSignature = "\"" + ModifyFunctionSignatureFormat(addedPart) + "\"";
            string oldFunctionSignature = "\"" + ModifyFunctionSignatureFormat(deletedPart.Value) + "\"";

            return (string.Format("{0},{1},{2},{3}", commitSHA, javaFile, oldFunctionSignature, newFunctionSignature));
        }

        private string DetectInlineParameterAdd(Commit commit, string fileChangedSection, Match deletedPart, string javaFile)
        {
            var addedPart = InlineMethodDeclarationAdded.Match(fileChangedSection);

            if (!IsChangedParameterCount(addedPart.Value, deletedPart.Value))
                return String.Empty;

            var commitSHA = "\"" + commit.Sha + "\"";
            var oldSignature = "\"" + ModifyFunctionSignatureFormat(deletedPart.Value) + "\"";
            var newSignature = "\"" + ModifyFunctionSignatureFormat(addedPart.Value) + "\"";

            return string.Format("{0},{1},{2},{3}", commitSHA, javaFile, oldSignature, newSignature);
        }

        private string ModifyFunctionSignatureFormat(string functionSignature)
        {
            var splittedPart = functionSignature.Split('(');
            var parameter = splittedPart[1].Trim();

            return Regex.Replace($"{splittedPart[0].TrimStart()}({parameter.Split(')')[0]})", @"\t|\n|\r|\s\s|\+|-","");
        }

        private bool IsChangedParameterCount(string newFunctionSignature, string oldFunctionSignature)
        {
            if (newFunctionSignature.Split(',').Length > oldFunctionSignature.Split(',').Length)
                return true;

            return false;
        }

        private void RepositorySelection_Click(object sender, RoutedEventArgs e)
        {
            var folderSelection = new FolderBrowserDialog();

            if (folderSelection.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (CheckFolderPathValidation(folderSelection.SelectedPath) == CheckResult.Valid)
                {
                    GitRepositoryPath = folderSelection.SelectedPath;
                    Results.Text += $"You Select {GitRepositoryPath} Path ...\n";
                }
            }
        }

        private CheckResult CheckFolderPathValidation(string path)
        {
            if (Directory.Exists(path) && Directory.Exists(path + "\\.git"))
                return CheckResult.Valid;
            else
                System.Windows.MessageBox.Show(".git Directory Does Not Exist in This Path.");

            return CheckResult.Invalid;
        }

        private void ResultPathSelction_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (.csv)|*.csv;";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ResultFilePath = openFileDialog.FileName;
                Results.Text += $"You Select {openFileDialog.FileName} File as Result File...\n";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var items = Regex.Split("\n@@ shabnan @@ test \n@@ djkswjed @@ hjhjaj" , @"\n@@");
            var x = items[0];
        }
    }
}
