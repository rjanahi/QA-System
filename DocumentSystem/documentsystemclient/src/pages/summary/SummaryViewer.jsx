
import { useEffect, useState } from "react";
import { summaryService } from "../../services/summaryService";
import "./SummaryViewer.css";

function SummaryViewer({ analysis, document, onBack }) {

    //Check whether the current analysis is a QA analysis
    const isQA = analysis.analysisType === "QA";

    //Store the type of summary selected by the user
    const [summaryType, setSummaryType] = useState("General");

    //Store all summaries belonging to this analysis
    const [history, setHistory] = useState([]);

    //Store the summary currently displayed
    const [selectedSummary, setSelectedSummary] = useState(null);

    //Track loading and AI generation operations
    const [loading, setLoading] = useState(true);
    const [generating, setGenerating] = useState(false);

    //Store success and error messages
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");


    //Load saved summaries when the viewer opens
    useEffect(() => {
        let active = true;

        async function loadHistory() {
            try {
                setLoading(true);

                //Retrieve summaries for this specific analysis
                const results =
                    await summaryService.getSummaryHistory(
                        analysis.analysisId
                    );

                //Don't update state after leaving the page
                if (!active) return;

                setHistory(results);

            } catch (err) {
                if (active) setError(err.message);

            } finally {
                if (active) setLoading(false);
            }
        }

        loadHistory();

        //Ignore the response if the component is unmounted
        return () => {
            active = false;
        };

    }, [analysis.analysisId]);


    //Keep the selected summary consistent with its type
    useEffect(() => {

        //Find summaries matching the selected type
        const matching = history.filter(
            (item) => item.summaryType === summaryType
        );

        //Select the most recently created matching summary
        const latest = [...matching].sort(
            (a, b) => b.version - a.version
        )[0];

        setSelectedSummary(latest || null);

    }, [summaryType, history]);


    //Generate a new summary from the selected analysis
    async function handleGenerate() {
        try {
            //Show loading state during AI generation
            setGenerating(true);

            //Clear old messages
            setMessage("");
            setError("");

            //Call the C# summary generation endpoint
            const generated = await summaryService.generateSummary(
                analysis.analysisId,
                summaryType
            );

            //Display the newly generated summary
            setSelectedSummary(generated);

            //Reload the summary history from SQL Server
            const updatedHistory =
                await summaryService.getSummaryHistory(
                    analysis.analysisId
                );

            setHistory(updatedHistory);

            //Show success after the request completes
            setMessage("Summary generated successfully.");

        } catch (err) {
            //Display backend errors
            setError(err.message);

        } finally {
            //Enable the Generate button again
            setGenerating(false);
        }
    }


    //Open a previously saved summary version
    async function handleSelectSummary(summaryId) {
        try {
            setLoading(true);
            setError("");

            //Retrieve the selected summary from SQL Server
            const result = await summaryService.getSummary(summaryId);

            //Display the selected version
            setSelectedSummary(result);

        } catch (err) {
            setError(err.message);

        } finally {
            setLoading(false);
        }
    }


    //Get saved versions for the selected summary type
    const filteredHistory = history
        .filter((item) => item.summaryType === summaryType)
        .sort((a, b) => b.version - a.version);


    return (
        <div className="summary-page">

            {/* Navigation and document information */}
            <header className="summary-header">
                <div>
                    <button
                        type="button"
                        className="summary-back-button"
                        onClick={onBack}
                    >
                        Back to Analysis
                    </button>

                    <h1>Document Summaries</h1>

                    <p>
                        {document.title} | Analysis Version {analysis.version}
                    </p>
                </div>
            </header>


            {/* Summary type selection and generation */}
            <section className="summary-card">

                <h2>Generate Summary</h2>

                <div className="summary-controls">

                    <div className="summary-field">
                        <label htmlFor="summary-type">
                            Summary Type
                        </label>

                        <select
                            id="summary-type"
                            value={summaryType}
                            disabled={generating || loading}
                            onChange={(event) => {
                                setSummaryType(event.target.value);
                                setMessage("");
                                setError("");
                            }}
                        >
                            <option value="General">
                                General Summary
                            </option>

                            {/* QA Documents support specialized summaries */}
                            {isQA && (
                                <>
                                    <option value="Technical">
                                        Technical Summary
                                    </option>

                                    <option value="Executive">
                                        Executive Summary
                                    </option>
                                </>
                            )}
                        </select>
                    </div>

                    <button
                        type="button"
                        onClick={handleGenerate}
                        disabled={generating || loading}
                    >
                        {generating
                            ? "Generating..."
                            : "Generate Summary"}
                    </button>

                </div>

                {/* Explain which summary is being generated */}
                <p className="summary-description">
                    {summaryType === "Technical"
                        ? "Detailed results for developers and QA engineers."
                        : summaryType === "Executive"
                            ? "High-level quality risks and priorities for managers."
                            : "A clear overview of the analysis and its findings."}
                </p>

            </section>


            {/* Display operation feedback */}
            {message && (
                <p className="summary-success" role="status">
                    {message}
                </p>
            )}

            {error && (
                <p className="summary-error" role="alert">
                    {error}
                </p>
            )}


            {/* Display previously generated versions */}
            {filteredHistory.length > 0 && (
                <section className="summary-card">

                    <h2>Summary History</h2>

                    <div className="summary-history">
                        {filteredHistory.map((item) => (
                            <button
                                key={item.id}
                                type="button"
                                disabled={loading || generating}
                                className={
                                    selectedSummary?.id === item.id
                                        ? "summary-version selected"
                                        : "summary-version"
                                }
                                onClick={() =>
                                    handleSelectSummary(item.id)
                                }
                            >
                                Version {item.version}
                            </button>
                        ))}
                    </div>

                </section>
            )}


            {/* Loading indicators */}
            {loading && <p>Loading summaries...</p>}

            {generating && (
                <p role="status">
                    AI is generating your {summaryType.toLowerCase()} summary...
                </p>
            )}


            {/* Display the selected saved summary */}
            {!loading && selectedSummary && (
                <section className="summary-card">

                    <div className="summary-result-header">
                        <div>
                            <h2>{selectedSummary.title}</h2>

                            <p className="summary-meta">
                                {selectedSummary.summaryType}
                                {" | "}
                                Version {selectedSummary.version}
                                {" | "}
                                {selectedSummary.status}
                            </p>
                        </div>
                    </div>

                    {/* Preserve paragraphs and line breaks */}
                    <div className="summary-content">
                        {selectedSummary.content}
                    </div>

                </section>
            )}


            {/* Empty state for summaries not generated yet */}
            {!loading &&
                !generating &&
                !selectedSummary &&
                !error && (
                    <section className="summary-card">
                        <h2>No Summary Yet</h2>

                        <p>
                            Select a summary type and click
                            Generate Summary to create one.
                        </p>
                    </section>
                )}

        </div>
    );
}

export default SummaryViewer;
