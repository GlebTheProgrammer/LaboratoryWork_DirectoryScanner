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
    }
}