using System.Diagnostics;
using System.Net.Http;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using Frends.HIT.RemoteFS;

namespace Frends.HIT.RemoteFS.Tests {
    public class ReadWriteTests {
        private ServerParams SParams;
        
        private string BaseFolder = "";

        public ReadWriteTests(ServerParams parames, string baseFolder = "") {
            SParams = parames;
            BaseFolder = baseFolder;
        }

        public async Task RunTests(string baseFolder) {
            BaseFolder = baseFolder;
            await TestListFiles();
            await TestWriteFile();
            await TestReadFile();
            await TestCopyFile();
            await TestDeleteFile();
        }

        private async Task TestListFiles() {
            ListParams paramets = new ListParams(){
                Path = this.BaseFolder,
                Filter = FilterTypes.None
            };


            var result = await Main.ListFiles(paramets, this.SParams);

            Console.WriteLine(result);
        }

        private async Task TestWriteFile() {
            WriteParams paramets = new WriteParams(){
                Path = BaseFolder,
                File = "test.txt",
                Content = "Hello, World!",
                Encoding = FileEncodings.UTF_8
            };

            var result = await Main.WriteFile(paramets, this.SParams);

            Debug.Assert(result.Success);
        }

        private async Task TestReadFile() {
            ReadParams paramets = new (){
                Path = BaseFolder,
                File = "test.txt",
                Encoding = FileEncodings.UTF_8
            };

            var result = await Main.ReadFile(paramets, this.SParams);

            Debug.Assert(result.Content == "Hello, World!");
        }

        private async Task TestCopyFile() {
            ReadParams paramets = new (){
                Path = BaseFolder,
                File = "test.txt",
                Encoding = FileEncodings.UTF_8
            };         

            CopyDestParams writeParams = new (){
                Path = BaseFolder,
                File = "test-copy.txt",
                Encoding = FileEncodings.UTF_8
            };

            var result = await Main.CopyFile(paramets, this.SParams, writeParams, this.SParams);

            Debug.Assert(result.Success);

            var readParams = new ReadParams(){
                Path = BaseFolder,
                File = "test-copy.txt",
                Encoding = FileEncodings.UTF_8
            };

            var readResult = await Main.ReadFile(readParams, this.SParams);

            Debug.Assert(readResult.Content == "Hello, World!");
        }

        private async Task TestDeleteFile() {
            DeleteParams paramets = new (){
                Path = BaseFolder,
                File = "test.txt",
            };

            var result = await Main.DeleteFile(paramets, this.SParams);

            Debug.Assert(result.Success);

            paramets.File = "test-copy.txt";

            result = await Main.DeleteFile(paramets, this.SParams);

            Debug.Assert(result.Success);
        }
    }


}