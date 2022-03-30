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

            await Program.Main(new[] { Path.Combine(directory, $"{projectDirectoryName}.sln"), nameof(GeneratedReportIsEqualToExpected) });

            var expected = File.ReadAllLines(
                Path.Combine(Directory.GetCurrentDirectory(), "Expected", $"{projectDirectoryName}.csv"));
            var actual = File.ReadAllLines(Path.Combine(directory, $"{projectDirectoryName}.csv"));
            
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}