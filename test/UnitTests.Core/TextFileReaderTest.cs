using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Text;

namespace Zonkey.UnitTests
{
    [TestClass, Ignore]
    public class TextClassReaderTest
    {
#if (false)
        [TestMethod]
        public void ReadPaveCampaign_Test()
        {
            using (var reader = new TextClassReader<PAVeCampaign>("C:\\Temp\\PaveImport\\_test1\\UD_01_Campaign_20090417073310.dat"))
            {
                foreach (PAVeCampaign campaign in reader)
                {
                    Console.WriteLine(campaign.campaign_id);
                    Console.WriteLine(campaign.campaign_name);
                    Console.WriteLine(campaign.campaign_date.ToShortDateString());
                    Console.WriteLine(campaign.expiration_date.ToString());
                    Console.WriteLine(campaign.order_for_agent_id);
                    Console.WriteLine(campaign.is_ocr);

                    Console.WriteLine();
                }
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ReadPaveCampaign_Filter_Test()
        {
            using (var reader = new TextClassReader<PAVeCampaign>("C:\\Temp\\PaveImport\\_test1\\UD_01_Campaign_20090417073310.dat"))
            {
                reader.LineFilter = ((line, str) => str.Substring(180, 6) == "322022");

                foreach (PAVeCampaign campaign in reader)
                {
                    Console.WriteLine(campaign.campaign_id);
                    Console.WriteLine(campaign.campaign_name);
                    Console.WriteLine(campaign.campaign_date.ToShortDateString());
                    Console.WriteLine(campaign.expiration_date.ToString());
                    Console.WriteLine(campaign.order_for_agent_id);
                    Console.WriteLine(campaign.is_ocr);

                    Console.WriteLine();
                }
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ReadPaveCampaign_Linq_Test()
        {
            using (var reader = new TextClassReader<PAVeCampaign>("C:\\Temp\\PaveImport\\_test1\\UD_01_Campaign_20090417073310.dat"))
            {
                var q = from c in reader
                        where c.orderer_agent_id == "4791FD" 
                        select c;

                foreach (PAVeCampaign campaign in q)
                {
                    Console.WriteLine(campaign.campaign_id);
                    Console.WriteLine(campaign.campaign_name);
                    Console.WriteLine(campaign.campaign_date.ToShortDateString());
                    Console.WriteLine(campaign.expiration_date.ToString());
                    Console.WriteLine(campaign.order_for_agent_id);
                    Console.WriteLine(campaign.is_ocr);

                    Console.WriteLine();
                }
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ReWritePaveCampaign_Linq_Test()
        {
            using (var reader = new TextClassReader<PAVeCampaign>("C:\\Temp\\PaveImport\\_test1\\UD_01_Campaign_20090417073310.dat"))
            {
                using (var writer = new TextClassWriter<PAVeCampaign>("C:\\Temp\\PaveImport\\_test1\\CampaingsForDaveC.dat"))
                {
                    var q = from c in reader
                            where c.orderer_agent_id == "4791FD"
                            select c;

                    writer.Write(q);
                }
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void ReadTextUsers()
        {
            using (var reader = new TextClassReader<TextUser>(@"W:\forKelly\CSV Reader Test\UserImport.csv"))
            {
                foreach (var user in reader)
                {
                    Console.WriteLine(user.FirstName);
                }
            }

            Assert.IsTrue(true);
        }
#endif
    }
}
