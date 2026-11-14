
import { useEffect, useState } from "react";
import { analysisService } from "../../services/analysisService";
import "./AnalysisViewer.css";
//Import the summary generation and viewing page
import SummaryViewer from "../summary/SummaryViewer";

function AnalysisViewer({ document, onBack }) {

    //Store previous analyses for this document
    const [history, setHistory] = useState([]);

    //Store the selected analysis result
    const [analysis, setAnalysis] = useState(null);

    //Track which operation is loading
    const [loading, setLoading] = useState(true);
    const [generating, setGenerating] = useState(false);

    //Store messages and errors
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    //Check whether the selected document is a QA Document
    const isQA = document.documentType === "QA";

    //Track whether the user is viewing document summaries
    const [showSummaries, setShowSummaries] = useState(false);


    //Load analysis history when opening the viewer
    useEffect(() => {
        let active = true;

        async function loadInitialHistory() {
            try {
                setLoading(true);

                //Get all previous analyses for this document
                const results =
                    await analysisService.getAnalysisHistory(document.id);

                if (!active) return;

                //Only show history for the selected document's analysis type
                const filteredHistory = results.filter(
                    (item) =>
                        item.analysisType === (isQA ? "QA" : "General")
                );

                setHistory(filteredHistory);

                //Load the newest analysis of the correct type
                if (filteredHistory.length > 0) {
                    const latest = await analysisService.getAnalysisResult(
                        filteredHistory[0].analysisId
                    );
                }

            } catch (err) {
                if (active) setError(err.message);
            } finally {
                if (active) setLoading(false);
            }
        }

        loadInitialHistory();

        //Ignore results if the user leaves this page
        return () => {
            active = false;
        };
    }, [document.id, isQA]);


    //Generate a new analysis based on document type
    async function handleGenerate() {
        try {
            //Show the loading state
            setGenerating(true);

            //Clear previous messages
            setError("");
            setMessage("");

            let result;

            //Choose the correct AI analysis service
            if (isQA) {

                //Generate a specialized QA analysis
                result = await analysisService.generateQAAnalysis(
                    document.id
                );

            } else {

                //Generate a general document analysis
                result = await analysisService.generateGeneralAnalysis(
                    document.id
                );
            }

            //Retrieve the complete saved analysis from SQL Server
            const saved = await analysisService.getAnalysisResult(
                result.analysisId
            );

            //Display the new analysis
            setAnalysis(saved);

            //Refresh the analysis version history
            const updatedHistory =
                await analysisService.getAnalysisHistory(document.id);

            //Keep history limited to this document's analysis type
            setHistory(
                updatedHistory.filter(
                    (item) =>
                        item.analysisType === (isQA ? "QA" : "General")
                )
            );

            //Display a success message
            setMessage(
                isQA
                    ? "QA analysis completed successfully."
                    : "General analysis completed successfully."
            );

        } catch (err) {

            //Display errors returned by the backend
            setError(err.message);

        } finally {

            //Enable the Generate button again
            setGenerating(false);
        }
    }


    //Open a previous analysis version
    async function handleSelectVersion(analysisId) {
        try {
            setLoading(true);
            setError("");
            setMessage("");

            //Load the selected analysis from SQL Server
            const result =
                await analysisService.getAnalysisResult(analysisId);

            setAnalysis(result);

        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    //Show summaries for the currently selected analysis
    if (showSummaries && analysis) {
        return (
            <SummaryViewer
                analysis={analysis}
                document={document}
                onBack={() => setShowSummaries(false)}
            />
        );
    }

    return (
        <div className="analysis-page">

            {/* Navigation and page title */}
            <header className="analysis-header">
                <div>
                    <button
                        type="button"
                        className="back-button"
                        onClick={onBack}
                    >
                        Back to Documents
                    </button>

                    {/* Display a title based on the document type */}
                    <h1>
                        {isQA ? "QA Document Analysis" : "Document Analysis"}
                    </h1>
                    <p>{document.title}</p>
                </div>

                {/* Allow users to create additional versions */}
                <button
                    type="button"
                    onClick={handleGenerate}
                    disabled={generating || loading}
                >
                    {generating
                        ? "Analyzing..."
                        : "Generate New Analysis"}
                </button>
            </header>


            {/* Display success and error messages */}
            {message && (
                <p className="analysis-success">{message}</p>
            )}

            {error && (
                <p className="analysis-error">{error}</p>
            )}


            {/* Show previous analysis versions */}
            {history.length > 0 && (
                <section className="analysis-card">
                    <h2>Analysis History</h2>

                    <div className="history-list">
                        {history.map((item) => (
                            <button
                                key={item.analysisId}
                                type="button"
                                disabled={loading || generating}
                                className={
                                    analysis?.analysisId === item.analysisId
                                        ? "history-button selected"
                                        : "history-button"
                                }
                                onClick={() =>
                                    handleSelectVersion(item.analysisId)
                                }
                            >
                                Version {item.version}
                                {" | "}
                                {item.analysisType}
                                {" | "}
                                {item.status}
                            </button>
                        ))}
                    </div>
                </section>
            )}


            {/* Loading state */}
            {loading && <p>Loading analysis...</p>}

            {/* AI generation state */}
            {generating && (
                <p>AI is analyzing the document...</p>
            )}


            {/* Show helpful message when there are no analyses */}
            {!loading && !generating &&
                !analysis && history.length === 0 && (
                    <section className="analysis-card">
                        <h2>No Analyses Yet</h2>

                        <p>
                            Click Generate New Analysis to analyze
                            this document using AI.
                        </p>
                    </section>
                )}


            {/* Show the selected analysis */}
            {!loading && analysis && (
                <>

                    {/* Open the summary viewer for the selected analysis */}
                    <section className="analysis-card">
                        <h2>AI-Generated Summaries</h2>

                        <p>
                            Generate a summary of this analysis or view
                            previously saved versions.
                        </p>

                        <button
                            type="button"
                            onClick={() => setShowSummaries(true)}
                            disabled={generating || loading}
                        >
                            View / Generate Summaries
                        </button>
                    </section>

                    {/* Show testing statistics only for saved QA analyses */}
                    {analysis.analysisType === "QA" && (

                      <section className="analysis-card">

                        <h2>Test Execution Statistics</h2>

                        <div className="qa-stats-grid">

                          {/* Total number of test cases */}
                          <div className="qa-stat-card">
                            <span>Total Tests</span>
                            <strong>{analysis.totalTests ?? "N/A"}</strong>
                          </div>

                          {/* Successfully executed tests */}
                          <div className="qa-stat-card qa-passed">
                            <span>Passed</span>
                            <strong>{analysis.passedTests ?? "N/A"}</strong>
                          </div>

                          {/* Failed test cases */}
                          <div className="qa-stat-card qa-failed">
                            <span>Failed</span>
                            <strong>{analysis.failedTests ?? "N/A"}</strong>
                          </div>

                          {/* Tests that could not proceed */}
                          <div className="qa-stat-card qa-blocked">
                            <span>Blocked</span>
                            <strong>{analysis.blockedTests ?? "N/A"}</strong>
                          </div>

                          {/* Tests that were skipped */}
                          <div className="qa-stat-card">
                            <span>Skipped</span>
                            <strong>{analysis.skippedTests ?? "N/A"}</strong>
                          </div>

                        </div>

                      </section>
                    )}

                
                    {/* Analysis overview */}
                    <section className="analysis-card">
                        <h2>
                            Analysis Results: Version {analysis.version}
                        </h2>

                        <p className="analysis-meta">
                            Type: {analysis.analysisType}
                            {" | "}
                            Status: {analysis.status}
                        </p>

                        <h3>Overview and Assessment</h3>

                        <p className="analysis-text">
                            {analysis.overallAssessment}
                        </p>
                    </section>


                    {/* Identified risks */}
                    <section className="analysis-card">
                        <h2>Key Risks</h2>

                        {analysis.keyRisks?.length > 0 ? (
                            <ul>
                                {analysis.keyRisks.map((risk, index) => (
                                    <li key={index}>{risk}</li>
                                ))}
                            </ul>
                        ) : (
                            <p>No key risks identified.</p>
                        )}
                    </section>


                    {/* AI recommendations */}
                    <section className="analysis-card">
                        <h2>Recommendations</h2>

                        {analysis.recommendations?.length > 0 ? (
                            <ul>
                                {analysis.recommendations.map(
                                    (recommendation, index) => (
                                        <li key={index}>{recommendation}</li>
                                    )
                                )}
                            </ul>
                        ) : (
                            <p>No recommendations available.</p>
                        )}
                    </section>


                    {/* Individual findings */}
                    <section className="analysis-card">
                        <h2>Detailed Findings</h2>

                        {analysis.findings?.length > 0 ? (
                            <div className="findings-list">
                                {analysis.findings.map((finding) => (
                                    <article
                                        key={finding.id}
                                        className="finding-card"
                                    >
                                        <div className="finding-heading">
                                            <h3>{finding.title}</h3>

                                            <span className="severity">
                                                {finding.severity}
                                            </span>
                                        </div>

                                        <p>{finding.description}</p>

                                        <p>
                                            <strong>Category:</strong>{" "}
                                            {finding.category}
                                        </p>

                                        {/* Show affected software functionality for QA findings */}
                                        {analysis.analysisType === "QA" &&
                                            finding.affectedArea && (
                                                <p>
                                                    <strong>Affected Area:</strong>{" "}
                                                    {finding.affectedArea}
                                                </p>
                                            )}

                                        <p>
                                            <strong>Impact:</strong>{" "}
                                            {finding.impact}
                                        </p>

                                        <p>
                                            <strong>Recommendation:</strong>{" "}
                                            {finding.recommendation}
                                        </p>

                                        <p>
                                            <strong>Source:</strong>{" "}
                                            {finding.sourceLocation}
                                        </p>
                                    </article>
                                ))}
                            </div>
                        ) : (
                            <p>No detailed findings identified.</p>
                        )}
                    </section>
                </>
            )}
        </div>
    );
}

export default AnalysisViewer;
