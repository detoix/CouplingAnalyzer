using NUnit.Framework;
using CouplingAnalyzer;
using System.IO;
using System.Threading.Tasks;

namespace CouplingAnalyzer.Tests
{
    public class Tests
    {
        [Test]
        public async Task GeneratedReportIsEqualToExpected()
        {
            var directory = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent}.Resources";
            var projectDirectoryName = Path.GetFileName(directory);
            var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), $"{projectDirectoryName}.tsv");
            var actualPath = Path.Combine(directory, $"{projectDirectoryName}.tsv");

            if (File.Exists(actualPath))
            {
                File.Delete(actualPath);
            }

            await Program.Main(new[] { Path.Combine(directory, $"{projectDirectoryName}.sln"), nameof(GeneratedReportIsEqualToExpected) });

            var expected = File.ReadAllLines(expectedPath);
            var actual = File.ReadAllLines(actualPath);
            
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}