namespace Scanner.Tests
{
    public class Tests
    {
        [Fact]
        public void WorkingVariable_WhenStoppedGauging_EqualsFalse()
        {
            MyScannerLibrary.DirScanner.StopProcessing();

            Assert.False(MyScannerLibrary.DirScanner.isWorking);
        }

        [Fact]
        public void WorkingVariable_WhenStartedGauging_EqualsTrue()
        {
            MyScannerLibrary.DirScanner.Scan("C:\\Users\\Gleb\\Desktop\\English\\current");

            Assert.True(MyScannerLibrary.DirScanner.isWorking);

            MyScannerLibrary.DirScanner.StopProcessing();
        }

        [Fact]
        public void StartGauging_WhenDirectoryDoesNotExists_ThrowAnException()
        {
            string dirPath = "abcdefghij...wxyz";

            Assert.Throws<Exception>(() => MyScannerLibrary.DirScanner.Scan(dirPath));
        }

        [Fact]
        public void StartGauging_WhenDirectoryPathEqualsNull_ThrowAnException()
        {
            string dirPath = null;

            Assert.Throws<Exception>(() => MyScannerLibrary.DirScanner.Scan(dirPath));
        }

        // This test works only for basic folders. When we work with folder wich has git system inside, there will be 
        // a lot of hidden files too and this test will fail
        [Fact]
        public void Gauge_DirectoryWith3FilesInside_ReturnListWith3Entities()
        {
            string dirPath = "C:\\Users\\Gleb\\Desktop\\English\\current";

            List<MyScannerLibrary.Entity> entities = MyScannerLibrary.DirScanner.Scan(dirPath);

            Assert.Equal(4, entities.Count); // 1 Head directory + 3 files inside = 4 entities total
        }
    }
}