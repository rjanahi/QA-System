
import { getCsrfToken } from "./authService";

//Base URL for the C# SummariesController
const API_URL = "/api/Summaries";

export const summaryService = {

    //Generate and save a new AI summary
    async generateSummary(analysisId, summaryType) {

        //Get the CSRF token for the authenticated session
        const csrfToken = await getCsrfToken();

        //Send the analysis ID and requested summary type
        const response = await fetch(`${API_URL}/generate`, {
            method: "POST",

            //Include the authentication cookie
            credentials: "include",

            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": csrfToken,
            },

            //Convert JavaScript data into JSON
            body: JSON.stringify({
                analysisId,
                summaryType,
            }),
        });

        //Read the server response
        const data = await response.json().catch(() => null);

        //Handle errors returned by the backend
        if (!response.ok) {
            throw new Error(
                data?.message ||
                `Summary generation failed (${response.status}).`
            );
        }

        //Return the newly saved summary
        return data;
    },


    //Get all summaries belonging to one analysis
    async getSummaryHistory(analysisId) {

        const response = await fetch(
            `${API_URL}/analysis/${analysisId}`,
            {
                credentials: "include",
            }
        );

        //Check whether the request succeeded
        if (!response.ok) {
            throw new Error("Could not load summary history.");
        }

        //Return the list of saved summaries
        return await response.json();
    },


    //Get one saved summary using its unique ID
    async getSummary(summaryId) {

        const response = await fetch(
            `${API_URL}/${summaryId}`,
            {
                credentials: "include",
            }
        );

        //Check whether the request succeeded
        if (!response.ok) {
            throw new Error("Could not load the selected summary.");
        }

        //Return the requested summary
        return await response.json();
    },
};
