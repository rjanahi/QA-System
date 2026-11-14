
import { getCsrfToken } from "./authService";

//Base address for document endpoints
const API_URL = "/api/Documents";

export const documentService = {

    //Get all documents belonging to the logged-in user
    async getDocuments() {

        //Send GET request to the backend
        const response = await fetch(API_URL, {
            credentials: "include",
        });

        //Check if the request failed
        if (!response.ok) {
            throw new Error("Failed to load documents.");
        }

        //Convert JSON response into JavaScript objects
        return await response.json();
    },


    //Upload a selected document to the backend
    async uploadDocument(file, title) {

        //Get a valid CSRF request token
        const csrfToken = await getCsrfToken();

        //Create multipart form data
        const formData = new FormData();

        //Add the document's title
        formData.append("title", title);

        //Add the actual file
        formData.append("file", file);

        //Send document to the upload endpoint
        const response = await fetch(`${API_URL}/upload`, {
            method: "POST",

            //Include the user's authentication cookie
            credentials: "include",

            //Send the antiforgery request token
            headers: {
                "X-CSRF-TOKEN": csrfToken,
            },

            //Send the file and title
            body: formData,
        });

        //Read the response from the API
        const data = await response.json().catch(() => null);

        //Handle unsuccessful uploads
        if (!response.ok) {
            throw new Error(
                data?.message ||
                data?.title ||
                `Upload failed (${response.status}).`
            );
        }

        //Return the uploaded document information
        return data;
    },

    //Process an uploaded document
    async processDocument(documentId) {

        //Get the CSRF token for this authenticated session
        const csrfToken = await getCsrfToken();

        //Send the processing request to the C# backend
        const response = await fetch(
            `${API_URL}/${documentId}/process`,
            {
                method: "POST",

                //Include the authentication cookie
                credentials: "include",

                //Include the antiforgery request token
                headers: {
                    "X-CSRF-TOKEN": csrfToken,
                },
            }
        );

        //Read the response from the backend
        const data = await response.json().catch(() => null);

        //Handle processing errors
        if (!response.ok) {
            throw new Error(
                data?.message ||
                `Document processing failed (${response.status}).`
            );
        }

        //Return the processing result
        return data;
    },

    //Classify a processed document using AI
    async classifyDocument(documentId) {

        //Get the CSRF token for the authenticated user
        const csrfToken = await getCsrfToken();

        //Send the classification request to the backend
        const response = await fetch(
            `${API_URL}/${documentId}/classify`,
            {
                method: "POST",

                //Send the authentication cookie
                credentials: "include",

                //Send the CSRF token to protect the request
                headers: {
                    "X-CSRF-TOKEN": csrfToken,
                },
            }
        );

        //Read the backend response
        const data = await response.json().catch(() => null);

        //Handle unsuccessful classification
        if (!response.ok) {
            throw new Error(
                data?.message ||
                `Document classification failed (${response.status}).`
            );
        }

        //Return the AI classification result
        return data;
    },

    //Archive a document without permanently deleting it
    async archiveDocument(documentId) {

        //Get the CSRF token for the authenticated session
        const csrfToken = await getCsrfToken();

        //Send a DELETE request to the C# backend
        const response = await fetch(
            `${API_URL}/${documentId}`,
            {
                method: "DELETE",

                //Include the authentication cookie
                credentials: "include",

                //Protect the request with the CSRF token
                headers: {
                    "X-CSRF-TOKEN": csrfToken,
                },
            }
        );

        //Read the backend response
        const data = await response.json().catch(() => null);

        //Handle unsuccessful archiving
        if (!response.ok) {
            throw new Error(
                data?.message ||
                `Failed to archive document (${response.status}).`
            );
        }

        //Return the successful response
        return data;
    },

};
