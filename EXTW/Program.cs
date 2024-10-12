namespace EXTW
{
    internal class Program
    {
        const string HELP_INSTRUCTIONS =
            "mkdir <filepath / filename>" +
            "\nrmdir <filepath / filename>" +
            "\ncd <filepath / filename>" +
            "\nls" +
            "\nwrite <filepath> \"<content>\" or write append <filepath> \"<content>\"" +
            "\ncat <filepath>" +
            "\ncp <source> <destination>" +
            "\nrm <filepath / filename>" +
            "\nimport <source> <destination> or import append <source> <destination>" +
            "\nexport <source> <destination>" +
            "\n" +
            "\nsb" +
            "\nhelp";

        static void Main()
        {
            string filePath;
            int blockSize, blockQuantity, inodeQuantity;

            filePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..")) + @"\fsys";
            blockSize = GetValidIntegerInput("Input block size: ", "Invalid block size parameter value.");
            blockQuantity = GetValidIntegerInput("Input block quantity: ", "Invalid block quantity parameter value.");
            inodeQuantity = GetValidIntegerInput("Input block inode quantity: ", "Invalid block size parameter value.");

            Console.WriteLine();

            EXTW fsys = new(filePath, (short)blockSize, blockQuantity, (short)inodeQuantity);

            bool inFileSys = true;

            Dictionary<string, Action<string[]>> commands = new()
            {
                { "mkdir", args => fsys.MakeDirectory(args[0]) },
                { "rmdir", args => fsys.RemoveDirectory(args[0]) },
                { "ls", args => fsys.MyListDirectoryFiles() },
                { "cd", args => fsys.ChangeDirectory(args[0]) },
                { "write", args =>
                    {
                        if(args[0] == "append")
                            fsys.AppendDataFile(args[1], args[2]);
                        else
                            fsys.MakeFile(args[0], args[1]);
                    }
                },
                { "cat", args => fsys.ShowFileContent(args[0]) },
                { "cp", args => fsys.CopyFileTo(args[0], args[1]) },
                { "rm", args => fsys.RemoveFile(args[0]) },
                { "import", args =>
                    {
                        if(args[0] == "append")
                            fsys.AppendImportedFile(args[1], args[2]);
                        else
                            fsys.ImportFile(args[0], args[1]);
                    } 
                },
                { "export", args => fsys.ExportFile(args[0], args[1]) },
                { "sb", args => fsys.SuperBlock.DisplayMetaData() },
                { "help", args => Console.WriteLine(HELP_INSTRUCTIONS) },
                { "exit", args => inFileSys = false }
            };

            Console.WriteLine($"{HELP_INSTRUCTIONS}\n\n");

            while(inFileSys)
            {
                Console.Write($"{fsys.CurrentFilePath} : ");

                string[] commandDissected;
                string command = GetValidStringInput("");
                
                commandDissected = EXTW.DissectCommand(command);

                if(commandDissected == null || commandDissected.Length == 0)
                {
                    Console.WriteLine("Invalid command.");
                    continue;
                }

                string mainCommand = commandDissected[0];
                string[] args = commandDissected.Length > 1 ? commandDissected[1..] : [];

                if(commands.TryGetValue(mainCommand, out var action))
                {
                    try
                    {
                        action.Invoke(args);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.WriteLine($"Insufficient arguments for {mainCommand} command.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while executing command: {ex.Message}");
                    }
                }
                else
                    Console.WriteLine("Invalid command.");
            }
        }
        private static int GetValidIntegerInput(string prompt, string errorMessage)
        {
            while (true)
            {
                Console.Write(prompt);

                if(int.TryParse(Console.ReadLine(), out int input))
                    return input;
                else
                    Console.WriteLine(errorMessage);
            }
        }
        private static string GetValidStringInput(string prompt)
        {
            string input;

            do
            {
                Console.Write(prompt);

                input = Console.ReadLine()!;
                if (string.IsNullOrWhiteSpace(input))
                    Console.WriteLine("Given input was null or with white spaces. Please try again");
            } while (string.IsNullOrWhiteSpace(input));

            return input;
        }
    }
}
