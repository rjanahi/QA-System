
const API_URL = "/api/Authentication";

let csrfToken = null;

export async function getCsrfToken() {
    if (csrfToken) {
        return csrfToken;
    }

    const response = await fetch(`${API_URL}/csrf-token`, {
        credentials: "include",
        cache: "no-store",
    });

    if (!response.ok) {
        throw new Error("Could not initialize request security.");
    }

    const data = await response.json();

    csrfToken = data.token;
    return csrfToken;
}

async function request(endpoint, options = {}) {
    const method = (options.method || "GET").toUpperCase();

    const headers = {
        "Content-Type": "application/json",
        ...options.headers,
    };

    if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
        headers["X-CSRF-TOKEN"] = await getCsrfToken();
    }

    const response = await fetch(`${API_URL}${endpoint}`, {
        ...options,
        method,
        credentials: "include",
        headers,
    });

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.message || `Request failed (${response.status})`
        );
    }

    return data;
}

export const authService = {
    register: (fullName, email, password) =>
        request("/register", {
            method: "POST",
            body: JSON.stringify({ fullName, email, password }),
        }),

    login: async (email, password) => {
        const result = await request("/login", {
            method: "POST",
            body: JSON.stringify({ email, password }),
        });

        // The user identity has changed.
        // The next POST must use a fresh antiforgery token.
        csrfToken = null;

        return result;
    },

    getCurrentUser: () => request("/me"),

    logout: async () => {
        const result = await request("/logout", {
            method: "POST",
        });

        csrfToken = null;

        return result;
    },
};
