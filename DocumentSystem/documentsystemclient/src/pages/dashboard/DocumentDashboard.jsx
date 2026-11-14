
import { useEffect, useState } from "react";
import { documentService } from "../../services/documentService";
import "./DocumentDashboard.css";

//Import the analysis results page
import AnalysisViewer from "../analysis/AnalysisViewer";

function DocumentDashboard({ user, onLogout }) {

    //Store documents received from SQL Server through the API
    const [documents, setDocuments] = useState([]);

    //Store the selected file
    const [selectedFile, setSelectedFile] = useState(null);

    //Store the document title entered by the user
    const [title, setTitle] = useState("");

    //Track loading and uploading operations
    const [loading, setLoading] = useState(true);
    const [uploading, setUploading] = useState(false);

    //Store the ID of the document currently being processed
    const [processingId, setProcessingId] = useState(null);

    //Store the ID of the document currently being classified
    const [classifyingId, setClassifyingId] = useState(null);

    //Store the ID of the document currently being archived
    const [archivingId, setArchivingId] = useState(null);

    //Store the document currently opened for analysis
    const [selectedDocument, setSelectedDocument] = useState(null);

    //Store the text entered in the document search box
    const [searchQuery, setSearchQuery] = useState("");

    //Store the currently selected document filter
    const [statusFilter, setStatusFilter] = useState("All");

    //Store success and error messages
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");


    //Retrieve documents when the dashboard opens
    useEffect(() => {
        loadDocuments();
    }, []);


    //Get documents belonging to the current user
    async function loadDocuments() {
        try {
            setLoading(true);
            setError("");

            //Call the C# DocumentsController
            const result = await documentService.getDocuments();

            //Update the documents displayed on the page
            setDocuments(result);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }


    //Handle the user's file selection
    function handleFileChange(event) {
        const file = event.target.files[0];

        setSelectedFile(file || null);

        //Use the filename as the default document title
        if (file) {
            setTitle(file.name.replace(/\.[^.]+$/, ""));
        }
    }


    //Upload the document when the form is submitted
    async function handleUpload(event) {
        event.preventDefault();

        //Make sure a file was selected
        if (!selectedFile) {
            setError("Please select a document.");
            return;
        }

        //Reject files larger than 10 MB
        if (selectedFile.size > 10 * 1024 * 1024) {
            setError("Maximum file size is 10 MB.");
            return;
        }

        try {
            setUploading(true);
            setError("");
            setMessage("");

            //Send the file and title to the backend
            await documentService.uploadDocument(
                selectedFile,
                title
            );

            //Display success message
            setMessage("Document uploaded successfully.");

            //Reset the upload form
            setSelectedFile(null);
            setTitle("");

            //Reload documents from SQL Server
            await loadDocuments();

        } catch (err) {
            setError(err.message);
        } finally {
            setUploading(false);
        }
    }


    //Process the selected document
    async function handleProcess(documentId) {

        try {
            //Remember which document is being processed
            setProcessingId(documentId);

            //Clear previous messages
            setError("");
            setMessage("");

            //Call the document processing endpoint
            const result = await documentService.processDocument(
                documentId
            );

            //Display the number of chunks generated
            setMessage(
                `Document processed successfully. ${result.chunksCreated} chunks created.`
            );

            //Retrieve updated document statuses from SQL Server
            await loadDocuments();

        } catch (err) {
            //Display errors returned from the backend
            setError(err.message);

        } finally {
            //Enable the processing button again
            setProcessingId(null);
        }
    }


    //Classify a processed document as General or QA
    async function handleClassify(documentId) {

        try {
            //Remember which document is being classified
            setClassifyingId(documentId);

            //Clear old messages
            setError("");
            setMessage("");

            //Call the ASP.NET Core classification endpoint
            const result = await documentService.classifyDocument(
                documentId
            );

            //Show the classification returned by the AI
            setMessage(
                `Document classified successfully as ${result.documentType}.`
            );

            //Reload documents to display the saved classification
            await loadDocuments();

        } catch (err) {
            //Display errors returned by the API
            setError(err.message);

        } finally {
            //Reset the classification loading state
            setClassifyingId(null);
        }
    }


    //Archive a document selected by the user
    async function handleArchive(documentId, documentTitle) {

        //Ask the user to confirm before archiving
        const confirmed = window.confirm(
            `Are you sure you want to archive "${documentTitle}"?`
        );

        //Stop if the user cancels
        if (!confirmed)
            return;

        try {
            //Remember which document is being archived
            setArchivingId(documentId);

            //Clear previous messages
            setError("");
            setMessage("");

            //Call the C# archive endpoint
            await documentService.archiveDocument(documentId);

            //Show confirmation after the backend succeeds
            setMessage("Document archived successfully.");

            //Reload active documents from SQL Server
            await loadDocuments();

        } catch (err) {
            //Display errors from the backend
            setError(err.message);

        } finally {
            //Reset the archiving state
            setArchivingId(null);
        }
    }


    //Count documents that finished text extraction
    const readyCount = documents.filter(
        (doc) => doc.status === "Ready"
    ).length;


    //Count documents waiting for processing
    const uploadedCount = documents.filter(
        (doc) => doc.status === "Uploaded"
    ).length;

    //Filter documents based on their title and selected status
    const filteredDocuments = documents.filter((document) => {

        //Check whether the title or filename matches the search
        const matchesSearch =
            document.title.toLowerCase().includes(
                searchQuery.toLowerCase()
            ) ||
            document.originalFileName.toLowerCase().includes(
                searchQuery.toLowerCase()
            );

        //Check whether the document matches the selected filter
        const matchesStatus =
            statusFilter === "All" ||
            document.status === statusFilter ||
            document.documentType === statusFilter;

        //Only display documents meeting both conditions
        return matchesSearch && matchesStatus;
    });

    //Show the analysis viewer when a document is selected
    if (selectedDocument) {
        return (
            <AnalysisViewer
                document={selectedDocument}
                onBack={() => setSelectedDocument(null)}
            />
        );
    }

    return (
        <div className="dashboard">

            {/* Dashboard header */}
            <header className="dashboard-header">
                <div>
                    <h1>Document Intelligence</h1>
                    <p>Welcome, {user.fullName}</p>
                </div>

                <button onClick={onLogout}>
                    Logout
                </button>
            </header>


            {/* Document statistics */}
            <section className="stats">

                <div className="stat-card">
                    <h3>Total Documents</h3>
                    <strong>{documents.length}</strong>
                </div>

                <div className="stat-card">
                    <h3>Ready</h3>
                    <strong>{readyCount}</strong>
                </div>

                <div className="stat-card">
                    <h3>Awaiting Processing</h3>
                    <strong>{uploadedCount}</strong>
                </div>

            </section>


            {/* Document upload form */}
            <section className="dashboard-card">

                <h2>Upload Document</h2>

                <p>Supported formats: PDF, DOCX and TXT (10 MB max)</p>

                <form onSubmit={handleUpload}>

                    <div className="form-group">
                        <label>Choose Document</label>

                        <input
                            type="file"
                            accept=".pdf,.docx,.txt"
                            onChange={handleFileChange}
                            required
                        />
                    </div>

                    <div className="form-group">
                        <label>Document Title</label>

                        <input
                            type="text"
                            value={title}
                            onChange={(event) =>
                                setTitle(event.target.value)
                            }
                            maxLength={200}
                            required
                        />
                    </div>

                    <button type="submit" disabled={uploading}>
                        {uploading ? "Uploading..." : "Upload Document"}
                    </button>

                </form>

                {/* Success and error messages */}
                {message && (
                    <p className="success-message">{message}</p>
                )}

                {error && (
                    <p className="error-message">{error}</p>
                )}

            </section>


            {/* Documents list */}
            <section className="dashboard-card">

                <div className="section-heading">
                    <h2>My Documents</h2>

                    <button
                        type="button"
                        onClick={loadDocuments}
                        disabled={loading}
                    >
                        Refresh
                    </button>
                </div>


                {/* Search and filter documents */}
                <div className="document-toolbar">

                    {/* Search documents by title or filename */}
                    <input
                        type="search"
                        placeholder="Search documents..."
                        value={searchQuery}
                        onChange={(event) =>
                            setSearchQuery(event.target.value)
                        }
                        aria-label="Search documents"
                    />

                    {/* Filter documents by processing status or type */}
                    <select
                        value={statusFilter}
                        onChange={(event) =>
                            setStatusFilter(event.target.value)
                        }
                        aria-label="Filter documents"
                    >
                        <option value="All">All Documents</option>
                        <option value="Uploaded">Awaiting Processing</option>
                        <option value="Processing">Processing</option>
                        <option value="Ready">Ready</option>
                        <option value="Failed">Failed</option>
                        <option value="General">General Documents</option>
                        <option value="QA">QA Documents</option>
                    </select>

                </div>


                {loading ? (
                    <p>Loading documents...</p>
                ) : documents.length === 0 ? (
                    <p>No documents uploaded yet.</p>
                ) : filteredDocuments.length === 0 ? (
                    <p>No documents match your search or filter.</p>
                ) : (
                    <div className="documents-list">

                        {filteredDocuments.map((document) => (

                            <div
                                className="document-row"
                                key={document.id}
                            >
                                <div>
                                    <strong>{document.title}</strong>

                                    <p>{document.originalFileName}</p>

                                    <small>
                                        {document.documentType}
                                        {" | "}
                                        {(document.fileSizeBytes / 1024).toFixed(1)}
                                        {" KB"}
                                    </small>
                                </div>


                                {/* Document status and available actions */}
                                <div className="document-actions">

                                    {/* Apply a visual style based on the document status */}
                                    <span
                                        className={`document-status status-${document.status.toLowerCase()}`}
                                    >
                                        {document.status === "Uploaded"
                                            ? "Awaiting Processing"
                                            : document.status}
                                    </span>

                                    {/* Only show Process when extraction hasn't completed */}
                                    {document.status === "Uploaded" && (
                                        <button
                                            type="button"
                                            onClick={() => handleProcess(document.id)}
                                            disabled={processingId !== null}
                                        >
                                            {processingId === document.id
                                                ? "Processing..."
                                                : "Process"}
                                        </button>
                                    )}

                                    {/* Allow retrying a failed extraction */}
                                    {document.status === "Failed" && (
                                        <button
                                            type="button"
                                            onClick={() => handleProcess(document.id)}
                                            disabled={processingId !== null}
                                        >
                                            {processingId === document.id
                                                ? "Retrying..."
                                                : "Retry"}
                                        </button>
                                    )}


                                    {/* Show Classify for processed documents without a classification */}
                                    {document.status === "Ready" &&
                                        document.documentType === "Unknown" && (
                                            <button
                                                type="button"
                                                onClick={() => handleClassify(document.id)}

                                                //Disable while processing or classification is running
                                                disabled={
                                                    classifyingId !== null ||
                                                    processingId !== null
                                                }
                                            >
                                                {classifyingId === document.id
                                                    ? "Classifying..."
                                                    : "Classify"}
                                            </button>
                                        )}


                                    {/* Allow analysis for both General and QA documents */}
                                    {document.status === "Ready" &&
                                        ["General", "QA"].includes(document.documentType) && (
                                            <button
                                                type="button"
                                                onClick={() => setSelectedDocument(document)}
                                            >
                                                {document.documentType === "QA"
                                                    ? "QA Analysis"
                                                    : "Analyze / View"}
                                            </button>
                                        )}


                                    {/* Allow the user to archive this document */}
                                    <button
                                        type="button"
                                        className="archive-button"

                                        //Pass the document ID and title to the handler
                                        onClick={() =>
                                            handleArchive(document.id, document.title)
                                        }

                                        //Prevent conflicting actions while archiving
                                        disabled={
                                            archivingId !== null ||
                                            processingId !== null ||
                                            classifyingId !== null ||
                                            document.status === "Processing"
                                        }
                                    >
                                        {archivingId === document.id
                                            ? "Archiving..."
                                            : "Archive"}
                                    </button>



                                </div>

                            </div>

                        ))}

                    </div>
                )}

            </section>
        </div>
    );
}

export default DocumentDashboard;
