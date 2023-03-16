using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Collection;
using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.OnBase;
using Microsoft.Extensions.Configuration;

namespace iText
{
    class iText
    {
        private static readonly IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile("secret.json")
            .Build();

        /* Version 2
         * 1. Get Case Number
         * 2. Run Query to get Docket IDs
         * 3. Run Web Service to get Base64
         * 4. Put the Query results and Base64 into a list
         * 5. Run iText to loop through list and create the PDF.
         */
        static void Main(string[] args)
        {
            InsertDebug("Just started iText");
            InsertDebug(args.ToString());

            string caseNumber = string.Empty;
            string courtFolderDirectory = string.Empty;
            string docketCode = string.Empty;
            string docketDate = string.Empty;
            string docketID = string.Empty;
            string docketSeq = string.Empty;
            string docketText = string.Empty;
            string courtViewServer = string.Empty;
            string courtViewDatabase = string.Empty;

            InsertDebug("Just created variables");

            ParseArgs(args, out caseNumber, out courtFolderDirectory, out docketCode, out docketDate, out docketID, out docketSeq, out docketText, out courtViewServer, out courtViewDatabase);

            InsertDebug("Just parsed args");


            Dictionary<string, string> data = new Dictionary<string, string>();


            for (int i = 0; i < 15; i++)
            {
                try
                {
                    data["base64"] = CallWebService(docketID);
                    InsertDebug("successfully got base64: " + data["base64"]);
                    break;
                }
                catch (Exception ex)
                {
                    InsertDebug("error getting base64 for docket: " + docketID);
                    InsertDebug("error getting base64: " + ex.ToString());
                }
                if (i == 14)
                {
                    InsertDebug("giving up base64 retrieval for docket: " + docketID);
                }
                System.Threading.Thread.Sleep(2000);
            }
            data["caseNumber"] = caseNumber;
            data["docketID"] = docketID;
            data["caseFileLocation"] = courtFolderDirectory + @"\" + caseNumber + ".pdf";
            data["fileDisplay"] = docketDate + " " + docketSeq + ".pdf";
            data["description"] = docketText;
            data["fileAttachmentName"] = docketID.Trim() + ".pdf";
            data["tempFilePath"] = config["tempFilePath"];
            data["tempPdfFilePath"] = config["tempPdfFilePath"];
            data["tempPdfFilePath2"] = config["tempPdfFilePath2"];
            data["courtViewServer"] = courtViewServer;
            data["courtViewDatabase"] = courtViewDatabase;


            InsertDebug(caseNumber);

            if (File.Exists(data["caseFileLocation"]) == false)
            {
                InsertDebug("case didn't exist " + caseNumber);
                try
                {
                    CreateNewPortfolio(data);
                }
                catch (Exception ex)
                {
                    InsertDebug("error creating portfolio for " + caseNumber + ": " + ex.ToString());
                    InsertDebug("error creating portfolio for " + caseNumber + " (inner): " + ex.InnerException.ToString());
                }
                InsertDebug("created pdf " + caseNumber);
                try
                {
                    InsertDebug("appending new pdf " + caseNumber);
                    AppendPortfolio(data);
                    InsertDebug("appended new pdf " + caseNumber);
                }
                catch (Exception ex)
                {
                    InsertDebug("error appending portfolio for " + caseNumber + ": " + ex.ToString());
                    InsertDebug("error appending portfolio for " + caseNumber + " (inner): " + ex.InnerException.ToString());
                }
            }
            else
            {
                try
                {
                    InsertDebug("appending pdf " + caseNumber);
                    AppendPortfolio(data);
                    InsertDebug("appended pdf " + caseNumber);
                }
                catch
                {

                }

            }
        }

        // Creates a new Portfolio and creates the PDF if exists from Base64 else creates blank PDF
        public static void CreateNewPortfolio(Dictionary<string, string> data)
        {
            InsertDebug("creating new portfolio for " + data["caseNumber"]);
            PdfWriter pdfWriter = new PdfWriter(data["caseFileLocation"]);
            InsertDebug("new pdfWriter for :" + data["caseFileLocation"]);
            //pdfWriter.SetCompressionLevel(9);
            PdfDocument pdfDoc = new PdfDocument(pdfWriter);
            InsertDebug("new pdfDoc");
            Document doc = new Document(pdfDoc);
            InsertDebug("new doc");

            doc.Add(new Paragraph(data["caseNumber"]));
            InsertDebug("added case number");

            PdfCollection collection = new PdfCollection();
            InsertDebug("new collection");
            collection.SetView(PdfCollection.DETAILS);
            InsertDebug("collection view set");

            PdfCollectionSchema schema = new PdfCollectionSchema();
            InsertDebug("new schema");

            // File Name
            PdfCollectionField field = new PdfCollectionField("Description", PdfCollectionField.DESC);
            InsertDebug("new field");
            field.SetVisibility(true);
            InsertDebug("vis true");
            field.SetOrder(2);
            InsertDebug("order set");
            schema.AddField("Description", field);
            InsertDebug("description added");

            // File Name
            field = new PdfCollectionField("Name", PdfCollectionField.FILENAME);
            InsertDebug("field set");
            field.SetVisibility(true);
            field.SetOrder(1);
            schema.AddField("Name", field);

            collection.SetSchema(schema);
            InsertDebug("collection schema set");

            collection.SetInitialDocument(config["initialDocumentFileName"]);
            InsertDebug("initial doc set");

            PdfCollectionSort sort = new PdfCollectionSort("Name");
            InsertDebug("new sort");

            sort.SetSortOrder(false);

            collection.SetSort(sort);

            pdfDoc.GetCatalog().SetCollection(collection);
            InsertDebug("got catalog");

            PdfWriter coverSheetPdf = new PdfWriter(data["tempPdfFilePath"]);
            PdfDocument coverSheetDoc = new PdfDocument(coverSheetPdf);
            Document coverSheet = new Document(coverSheetDoc);

            string title = "";

            InsertDebug("new coversheet");
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    title = GetCaseCaption(data["caseNumber"], data["courtViewServer"], data["courtViewDatabase"]);
                    InsertDebug("successfully got title" + title);
                    break;
                }
                catch (Exception ex)
                {
                    InsertDebug("error getting title for case " + data["caseNumber"] + ": " + ex.ToString());
                    InsertDebug("error getting title inner exception: " + ex.InnerException.ToString());
                }
                if (i == 14)
                {
                    InsertDebug("giving up getting title for: " + data["caseNumber"]);
                }
                System.Threading.Thread.Sleep(2000);
            }
            InsertDebug("title set");

            Paragraph paragraph = new Paragraph();
            InsertDebug("new paragraph");

            paragraph.Add(data["caseNumber"]);
            paragraph.Add(new Tab());
            paragraph.Add(new Tab());
            paragraph.Add(title);

            InsertDebug("paragraph done");
            coverSheet.Add(paragraph);
            coverSheet.Close();
            coverSheetDoc.Close(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
            coverSheetPdf.Close(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
            coverSheet.Flush(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
            coverSheetPdf.Dispose(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
            InsertDebug("coversheet done");

            PdfFileSpec pdfFileSpecCover = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, data["tempPdfFilePath"], config["initialDocumentName"], config["initialDocumentFileName"], PdfName.ApplicationPdf, null, null);
            pdfDoc.AddFileAttachment(config["initialDocumentFileName"], pdfFileSpecCover);
            InsertDebug("added attachment");


            // Check if base64 is not null.
            /*
            if (data["base64"] != null)
            {
                // Base64 then convert to byte and add PDF to new portfolio.
                byte[] bytes = Convert.FromBase64String(data["base64"]);
                PdfFileSpec pdfFileSpec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, bytes, data["description"], data["fileDisplay"], PdfName.ApplicationPdf, null, null);
                pdfDoc.AddFileAttachment(data["fileAttachmentName"], pdfFileSpec);
            }
            else
            {
                // No Base64. Create Portfolio with a PDF that states that the Document could not be found in OnBase
                PdfWriter noPdf = new PdfWriter(data["tempPdfFilePath"]);
                //noPdf.SetCompressionLevel(9);
                PdfDocument noPdfDoc = new PdfDocument(noPdf);
                Document noDoc = new Document(noPdfDoc);
                noDoc.Add(new Paragraph("Docket ID: " + data["docketID"] + " could not be retrieved from OnBase"));
                noDoc.Close();

                PdfFileSpec pdfFileSpec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, data["tempPdfFilePath"], data["description"], data["fileDisplay"], PdfName.ApplicationPdf, null, null);
                pdfDoc.AddFileAttachment(data["fileAttachmentName"], pdfFileSpec);

            }
            */

            doc.Close();
            InsertDebug("doc closed");

            for (int i = 0; i < 15; i++)
            {
                try
                {
                    UpdateEventDockets(data["docketID"], data["courtViewServer"], data["courtViewDatabase"]);
                    InsertDebug("successfully updated event dockets");
                    break;
                }
                catch (Exception ex)
                {
                    InsertDebug("error updating event dockets on docketID " + data["docketID"] + ": " + ex.ToString());
                    InsertDebug("error updating event dockets inner exception: " + ex.InnerException.ToString());
                }
                if (i == 14)
                {
                    InsertDebug("giving up updating event dockets for: " + data["docketID"]);
                }
                System.Threading.Thread.Sleep(2000);
            }
            InsertDebug("updated events");
        }

        public static void AppendPortfolio(Dictionary<string, string> data)
        {
            InsertDebug("In AppendPortfolio");
            File.Copy(data["caseFileLocation"], data["tempFilePath"], true);

            InsertDebug("copied file " + data["caseNumber"]);
            PdfWriter pdfWriter = new PdfWriter(data["caseFileLocation"]);
            //pdfWriter.SetCompressionLevel(9);

            PdfDocument pdfDoc = new PdfDocument(new PdfReader(data["tempFilePath"]), pdfWriter);

            if (data["base64"] != null)
            {
                InsertDebug("got base64 " + data["caseNumber"]);
                // Base64 then convert to byte and add PDF to portfolio.
                byte[] bytes = Convert.FromBase64String(data["base64"]);
                PdfFileSpec pdfFileSpec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, bytes, data["description"], data["fileDisplay"], PdfName.ApplicationPdf, null, null);
                pdfDoc.AddFileAttachment(data["fileAttachmentName"], pdfFileSpec);
                InsertDebug("add file attachment " + data["caseNumber"]);
            }
            else
            {
                InsertDebug("no base64 " + data["caseNumber"]);
                // No Base64. Create Portfolio with a PDF that states that the Document could not be found in OnBase
                PdfWriter noPdf = new PdfWriter(data["tempPdfFilePath2"]);
                InsertDebug("noPdf created");
                //noPdf.SetCompressionLevel(9);
                PdfDocument noPdfDoc = new PdfDocument(noPdf);
                Document noDoc = new Document(noPdfDoc);
                noDoc.Add(new Paragraph("Docket ID: " + data["docketID"] + " could not be retrieved from OnBase"));
                noDoc.Close();
                noPdfDoc.Close(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
                noPdf.Close(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
                noPdf.Dispose(); // added 2021-11-09 because got an error tempPdf.pdf was in use by another process
                InsertDebug("noDoc closed " + data["caseNumber"]);
                PdfFileSpec pdfFileSpec = PdfFileSpec.CreateEmbeddedFileSpec(pdfDoc, data["tempPdfFilePath2"], data["description"], data["fileDisplay"], PdfName.ApplicationPdf, null, null);
                pdfDoc.AddFileAttachment(data["fileAttachmentName"], pdfFileSpec);

                InsertDebug("added attachment " + data["caseNumber"]);
            }

            pdfDoc.Close();

            InsertDebug("heading from AppendPortfolio to UpdateEventDockets");
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    UpdateEventDockets(data["docketID"], data["courtViewServer"], data["courtViewDatabase"]);
                    InsertDebug("successfully updated event dockets");
                    break;
                }
                catch (Exception ex)
                {
                    InsertDebug("error updating event dockets on docketID " + data["docketID"] + ": " + ex.ToString());
                    InsertDebug("error updating event dockets inner exception: " + ex.InnerException.ToString());
                }
                if (i == 14)
                {
                    InsertDebug("giving up updating event dockets for: " + data["docketID"]);
                }
                System.Threading.Thread.Sleep(2000);
            }
            InsertDebug("back to AppendPortfolio");
        }

        // Web call to get Base64 from OnBase
        public static string CallWebService(string docketID)
        {
            InsertDebug("In CallWebService for :" + docketID);

            string username = config["OnBaseUser"];
            string password = config["OnBasePassword"];

            OnBaseService onBaseService = new OnBaseService();
            InsertDebug("Created OnBaseService");

            Parameter[] parameters = new Parameter[1];
            parameters[0] = new Parameter();
            parameters[0].ParameterName = PARAM_NAMES.DocketId;
            parameters[0].ParameterValue = docketID;

            ReturnDocResponse results = onBaseService.GetBase64Documents(parameters, username, password);
            InsertDebug("Ran onBaseService.GetBase64Documents");

            if (results.ReturnDocs != null)
            {
                string base64 = results.ReturnDocs[0].Base64Document;
                return base64;
            }
            else
            {
                return null;
            }
        }


        // Adds the individual docket information from AK_DisasterRecoveryEventDocketsImport to AK_DisasterRecoveryEventDockets
        public static void UpdateEventDockets(string docketID, string courtViewServer, string courtViewDatabase)
        {
            InsertDebug("in UpdateEventDockets");
            string connStr = "Data Source=" + courtViewServer + ";Initial Catalog=" + courtViewDatabase + ";Integrated Security=SSPI;";
            SqlConnection conn = new SqlConnection(connStr);

            conn.Open();

            SqlCommand sql = new SqlCommand(config["updateEventsStoredProcedureName"], conn);
            sql.CommandType = CommandType.StoredProcedure;
            sql.Parameters.AddWithValue("dkt_id", docketID);
            sql.ExecuteNonQuery();

            conn.Close();

            InsertDebug("EventDockets updated");
        }

        // Get Case Caption
        public static string GetCaseCaption(string caseNumber, string courtViewServer, string courtViewDatabase)
        {
            InsertDebug("in getCaseCaption");
            string connStr = "Data Source=" + courtViewServer + ";Initial Catalog=" + courtViewDatabase + ";Integrated Security=SSPI;";
            InsertDebug("connection string: " + connStr);
            SqlConnection conn = new SqlConnection(connStr);
            InsertDebug("connection created");

            conn.Open();
            InsertDebug("connection open");

            string query = config["selectCaseCaptionFromCaseNumberQuery"] + " = '" + caseNumber + "'";

            InsertDebug(query);
            string title = "";
            SqlCommand sql = new SqlCommand(query, conn);
            InsertDebug("created sqlCommand");
            using (SqlDataReader reader = sql.ExecuteReader())
            {
                InsertDebug("created reader");
                while (reader.Read())
                {
                    InsertDebug("reading");
                    title = String.Format("{0}", reader[0]);
                    InsertDebug("read " + title);
                }
            }
            conn.Close();
            InsertDebug("got title" + title);

            return title;
        }

        // Parse data from the Execute Process Task
        private static void ParseArgs(string[] args, out string caseNumber, out string courtFolderDirectory, out string docketCode, out string docketDate, out string docketID, out string docketSeq, out string docketText, out string courtViewServer, out string courtViewDatabase)
        {
            InsertDebug("parsing args");

            caseNumber = string.Empty;
            courtFolderDirectory = string.Empty;
            docketCode = string.Empty;
            docketDate = string.Empty;
            docketID = string.Empty;
            docketSeq = string.Empty;
            docketText = string.Empty;
            courtViewServer = string.Empty;
            courtViewDatabase = string.Empty;

            string lastArg = string.Empty;
            bool foundNext = false;

            int paramCount = 0;

            InsertDebug("Just setup variables in ParseArgs");

            // Loop through the args[] array. 
            for (int i = 0; i <= args.GetUpperBound(0); i++)
            {
                foundNext = false;

                if (args[i] == "-caseNumber")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-caseNumber";
                    if (i > args.GetUpperBound(0)) break;
                    caseNumber = args[i];
                }
                if (args[i] == "-courtFolderDirectory")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-courtFolderDirectory";
                    if (i > args.GetUpperBound(0)) break;
                    courtFolderDirectory = args[i];
                }
                if (args[i] == "-docketCode")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-docketCode";
                    if (i > args.GetUpperBound(0)) break;
                    docketCode = args[i];
                }
                if (args[i] == "-docketDate")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-docketDate";
                    if (i > args.GetUpperBound(0)) break;
                    docketDate = args[i];
                }
                if (args[i] == "-docketID")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-docketID";
                    if (i > args.GetUpperBound(0)) break;
                    docketID = args[i];
                }
                if (args[i] == "-docketSeq")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-docketSeq";
                    if (i > args.GetUpperBound(0)) break;
                    docketSeq = args[i];
                }
                if (args[i] == "-docketText")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-docketText";
                    if (i > args.GetUpperBound(0)) break;
                    docketText = args[i];
                }
                if (args[i] == "-courtViewServer")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-courtViewServer";
                    if (i > args.GetUpperBound(0)) break;
                    courtViewServer = args[i];
                }
                if (args[i] == "-courtViewDatabase")
                {
                    i++;
                    paramCount++;
                    foundNext = true;
                    lastArg = "-courtViewDatabase";
                    if (i > args.GetUpperBound(0)) break;
                    courtViewDatabase = args[i];
                }

                if (!foundNext)
                {
                    // In case a parameter value contains spaces, it is spread over multiple elements in the args[] array.
                    // In this case we use lastArg to concatenate these different parts of the value to a single value.
                    switch (lastArg)
                    {
                        case "-caseNumber":
                            caseNumber = string.Format("{0} {1}", caseNumber, args[i]);
                            break;
                        case "-courtFolderDirectory":
                            courtFolderDirectory = string.Format("{0} {1}", courtFolderDirectory, args[i]);
                            break;
                        case "-docketCode":
                            docketCode = string.Format("{0} {1}", docketCode, args[i]);
                            break;
                        case "-docketDate":
                            docketDate = string.Format("{0} {1}", docketDate, args[i]);
                            break;
                        case "-docketID":
                            docketID = string.Format("{0} {1}", docketID, args[i]);
                            break;
                        case "-docketSeq":
                            docketSeq = string.Format("{0} {1}", docketSeq, args[i]);
                            break;
                        case "-docketText":
                            docketText = string.Format("{0} {1}", docketText, args[i]);
                            break;
                        case "-courtViewServer":
                            docketText = string.Format("{0} {1}", docketText, args[i]);
                            break;
                        case "-courtViewDatabase":
                            docketText = string.Format("{0} {1}", docketText, args[i]);
                            break;
                        default:
                            break;
                    }
                }
            }
            InsertDebug("Ending ParseArgs");
        }

        private static void InsertDebug(string debugText)
        {
            try
            {
                SqlConnection dbConn = new SqlConnection(config["connectionString"]); 

                dbConn.Open();
                SqlCommand insertDebug = new SqlCommand(config["insertDebugCommand"], dbConn);
                insertDebug.CommandType = CommandType.Text;
                insertDebug.Parameters.AddWithValue("debugText", debugText);
                insertDebug.ExecuteNonQuery();
                dbConn.Close();
            }
            catch { }
        }
    }
}
