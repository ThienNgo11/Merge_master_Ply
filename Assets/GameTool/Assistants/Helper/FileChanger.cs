#if !Minify
using System;
using System.IO;

namespace GameTool.Assistants.Helper
    {
        public class FileChanger
        {
            
            public static void WriteFile(string filePath, string _createClass, string _createPublicValue , string _createSetDefaultString , string _createGetDataString, string _createValidateString, string _initDefaultValue)
            {
                string line = "";
                string result = "";
    
                try
                {
                    StreamReader sr = new StreamReader(filePath);
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        result += line + "\n";
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
                finally
                {
                    Console.WriteLine("Executing finally block.");
                }
    
    
                int pos3 = result.IndexOf("//CreateClassContructorStart");
                int pos4 = result.IndexOf("//CreateClassContructorEnd");
                
                string replaceString2 = result.Substring(pos3, pos4 - pos3 + 26);
    
                string createPublicValue = result.Replace(replaceString2, "");
                string createPublicValue3 = createPublicValue.Insert(pos3, "//CreateClassContructorStart" + _createPublicValue + "\n \n //CreateClassContructorEnd");
    
    
    
                int pos1 = createPublicValue3.IndexOf("//CreateClassStart");
                int pos2 = createPublicValue3.IndexOf("//CreateClassEnd");
                string replaceString = createPublicValue3.Substring(pos1, pos2 - pos1 + 16);
    
                string createPublicValue4 = createPublicValue3.Replace(replaceString, "");
                string createPublicValue5 = createPublicValue4.Insert(pos1, "//CreateClassStart " + _createClass + "\n \n //CreateClassEnd");
    
    
                int pos5 = createPublicValue5.IndexOf("//adddefaultstart");
                int pos6 = createPublicValue5.IndexOf("//adddefaultend");
                string replaceString3 = createPublicValue5.Substring(pos5, pos6 - pos5 + 16);
    
                string createPublicValue6 = createPublicValue5.Replace(replaceString3, "");
                string createPublicValue7 = createPublicValue6.Insert(pos5, "//adddefaultstart \n" + _createSetDefaultString + "\n \n //adddefaultend\n");
                
    
                int pos7 = createPublicValue7.IndexOf("//getremotedatastart");
                int pos8 = createPublicValue7.IndexOf("//getremotedataend");
                string replaceString4 = createPublicValue7.Substring(pos7, pos8 - pos7 + 18);
    
                string createPublicValue8 = createPublicValue7.Replace(replaceString4, "");
                string createPublicValue9 = createPublicValue8.Insert(pos7, "//getremotedatastart \n" + _createGetDataString + "\n \n //getremotedataend");
                
                int pos9 = createPublicValue9.IndexOf("//updateonvalidatestart");
                int pos10 = createPublicValue9.IndexOf("//updateonvalidateend");
                string replaceString5 = createPublicValue9.Substring(pos9, pos10 - pos9 + 21);
    
                string createPublicValue10 = createPublicValue9.Replace(replaceString5, "");
                string createPublicValue11 = createPublicValue10.Insert(pos9, "//updateonvalidatestart \n" + _createValidateString + "\n \n //updateonvalidateend");
    
    
                int pos11 = createPublicValue11.IndexOf("//initdefaultstart");
                int pos12 = createPublicValue11.IndexOf("//initdefaultend");
                string replaceString6 = createPublicValue11.Substring(pos11, pos12 - pos11 + 16);
    
                string createPublicValue12 = createPublicValue11.Replace(replaceString6, "");
                string createPublicValue13 = createPublicValue12.Insert(pos11, "//initdefaultstart \n" + _initDefaultValue + "\n \n //initdefaultend");
    
                try
                {
                    //Pass the filepath and filename to the StreamWriter Constructor
                    StreamWriter sw = new StreamWriter(filePath);
                    //Write a line of text
                    sw.WriteLine(createPublicValue13);
                    sw.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
                finally
                {
                    Console.WriteLine("Executing finally block.");
                }
    
    
            }
    
            static void Main(string filePath)
            {
                int line_to_edit = 2;
                string sourceFile = "source.txt";
                string destinationString = "";
    
                // Read the appropriate line from the file.
                string lineToWrite = null;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    for (int i = 1; i <= line_to_edit; ++i)
                        lineToWrite = reader.ReadLine();
                }
    
                if (lineToWrite == null)
                    throw new InvalidDataException("Line does not exist in " + sourceFile);
    
                // Read the old file.
                string[] lines = File.ReadAllLines(destinationString);
    
                // Write the new file over the old file.
                using (StreamWriter writer = new StreamWriter(destinationString))
                {
                    for (int currentLine = 1; currentLine <= lines.Length; ++currentLine)
                    {
                        if (currentLine == line_to_edit)
                        {
                            writer.WriteLine(lineToWrite);
                        }
                        else
                        {
                            writer.WriteLine(lines[currentLine - 1]);
                        }
                    }
                }
            }
    
            static long CountLinesInFile(string filePath)
            {
                long count = 0;
                using (StreamReader r = new StreamReader(filePath))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
#endif