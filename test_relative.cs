using System;
using System.IO;

var basePath = "/invalid/path\\with*invalid<>chars";
var fullPath = "/home/user/documents/file.txt";

Console.WriteLine("basePath: " + basePath);
Console.WriteLine("fullPath: " + fullPath);

try {
    var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
    Console.WriteLine("baseUri: " + baseUri);
} catch (Exception ex) {
    Console.WriteLine("Exception creating baseUri: " + ex.GetType().Name);
}

try {
    var fullUri = new Uri(Path.GetFullPath(fullPath));
    Console.WriteLine("fullUri: " + fullUri);
} catch (Exception ex) {
    Console.WriteLine("Exception creating fullUri: " + ex.GetType().Name);
}
