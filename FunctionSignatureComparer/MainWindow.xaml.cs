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

namespace FunctionSignatureComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string GitRepositoryPath { get; set; }
        private string ResultFilePath { get; set; }

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
            string commitSHA = string.Empty;
            string JavaFile = string.Empty;
            string oldFunctionSignature = string.Empty;
            string newFunctionSignature = string.Empty;

            using (var repo = new Repository(GitRepositoryPath))
            {
                foreach (var commit in repo.Head.Commits)
                {
                    var commitTree = commit.Tree;
                    var parentCommitTree = commit?.Parents.FirstOrDefault()?.Tree;

                    if (commit.Parents.Count() > 1)
                        Results.Text += "I have more than one parent";

                    if (parentCommitTree == null)
                        return;

                    TreeChanges treeChanges = repo.Diff.Compare(parentCommitTree, commitTree);

                    foreach (TreeEntryChanges treeEntryChanges in treeChanges)
                    {
                        var AllChangedLines = treeEntryChanges.Patch.Split('\n').Where(s => s.StartsWith("-") || s.StartsWith("+")).ToList();
                        for (int i = 0; i < AllChangedLines.Count(); i++)
                        {

                            if (AllChangedLines[i].Contains("public") || AllChangedLines[i].Contains("private") || AllChangedLines[i].Contains("protected"))
                            {
                                var functionName = AllChangedLines[i].Split('(')[0].Replace("-", "+");
                                if (i + 1 >= AllChangedLines.Count())
                                    continue;

                                if (AllChangedLines[i + 1].Trim().Contains(functionName.Trim()))
                                {
                                    if (AllChangedLines[i].Split(',').Length != AllChangedLines[i + 1].Split(',').Length)
                                    {
                                        commitSHA = "\"" + commit.Sha + "\"";
                                        JavaFile = "\"" + treeEntryChanges.Path + "\"";
                                        oldFunctionSignature = "\"" + AllChangedLines[i].Split(')')[0].Replace("-", "") + ")\"";
                                        newFunctionSignature = "\"" + AllChangedLines[i+1].Split(')')[0].Replace("+", "") + ")\"";

                                        finalResult.WriteLine(string.Format("{0},{1},{2},{3}", commitSHA, JavaFile, oldFunctionSignature, newFunctionSignature));
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(finalResult.ToString()))
                        File.WriteAllText(resultFilePath, finalResult.ToString(), new UTF8Encoding());
                }

            }
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
            openFileDialog.Filter = "EXCEL Files (.xlsx)|*.xslx;";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ResultFilePath = openFileDialog.FileName;
                Results.Text += $"You Select {openFileDialog.FileName} File as Result File...\n";
            }
        }

    }
}
