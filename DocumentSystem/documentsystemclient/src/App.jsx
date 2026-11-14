
import { useEffect, useState } from "react";
import { authService } from "./services/authService";
import "./App.css";

//Import the authenticated document dashboard
import DocumentDashboard from "./pages/dashboard/DocumentDashboard";

function App() {
    const [user, setUser] = useState(null);
    const [mode, setMode] = useState("login");

    const [form, setForm] = useState({
        fullName: "",
        email: "",
        password: "",
    });

    const [message, setMessage] = useState("");
    const [loading, setLoading] = useState(false);
    const [checkingAuth, setCheckingAuth] = useState(true);

    // Check whether the user is already signed in
    useEffect(() => {
        authService
            .getCurrentUser()
            .then(setUser)
            .catch(() => setUser(null))
            .finally(() => setCheckingAuth(false));
    }, []);

    function handleChange(event) {
        const { name, value } = event.target;

        setForm((previous) => ({
            ...previous,
            [name]: value,
        }));
    }

    async function handleSubmit(event) {
        event.preventDefault();

        setLoading(true);
        setMessage("");

        try {
            if (mode === "register") {
                await authService.register(
                    form.fullName,
                    form.email,
                    form.password
                );

                setMode("login");
                setForm({
                    fullName: "",
                    email: form.email,
                    password: "",
                });

                setMessage("Account created! You can now log in.");
            } else {
                await authService.login(
                    form.email,
                    form.password
                );

                const currentUser =
                    await authService.getCurrentUser();

                setUser(currentUser);

                setForm({
                    fullName: "",
                    email: "",
                    password: "",
                });
            }
        } catch (error) {
            setMessage(error.message);
        } finally {
            setLoading(false);
        }
    }

    async function handleLogout() {
        setLoading(true);

        try {
            await authService.logout();
            setUser(null);
            setMode("login");
            setMessage("");
        } catch (error) {
            setMessage(error.message);
        } finally {
            setLoading(false);
        }
    }

    if (checkingAuth) {
        return <div className="auth-container">Loading...</div>;
    }

    //Display the document dashboard after login
    if (user) {
        return (
            <DocumentDashboard
                user={user}
                onLogout={handleLogout}
            />
        );
    }

    // Login / registration view
    return (
        <div className="auth-container">
            <div className="auth-card">
                <h1>Document Intelligence</h1>

                <p className="subtitle">
                    AI Document Analysis & Chat
                </p>

                <h2>
                    {mode === "login" ? "Welcome Back" : "Create Account"}
                </h2>

                <form onSubmit={handleSubmit}>
                    {mode === "register" && (
                        <div className="form-group">
                            <label>Full Name</label>
                            <input
                                type="text"
                                name="fullName"
                                value={form.fullName}
                                onChange={handleChange}
                                required
                                maxLength={150}
                            />
                        </div>
                    )}

                    <div className="form-group">
                        <label>Email</label>
                        <input
                            type="email"
                            name="email"
                            value={form.email}
                            onChange={handleChange}
                            required
                            autoComplete="username"
                        />
                    </div>

                    <div className="form-group">
                        <label>Password</label>
                        <input
                            type="password"
                            name="password"
                            value={form.password}
                            onChange={handleChange}
                            required
                            minLength={mode === "register" ? 8 : undefined}
                            autoComplete={
                                mode === "login"
                                    ? "current-password"
                                    : "new-password"
                            }
                        />
                    </div>

                    {message && <p className="message">{message}</p>}

                    <button type="submit" disabled={loading}>
                        {loading
                            ? "Please wait..."
                            : mode === "login"
                                ? "Login"
                                : "Register"}
                    </button>
                </form>

                <p className="switch-mode">
                    {mode === "login"
                        ? "Don't have an account?"
                        : "Already have an account?"}

                    <button
                        type="button"
                        className="text-button"
                        onClick={() => {
                            setMode(mode === "login" ? "register" : "login");
                            setMessage("");
                            setForm({
                                fullName: "",
                                email: "",
                                password: "",
                            });
                        }}
                    >
                        {mode === "login" ? "Register" : "Login"}
                    </button>
                </p>
            </div>
        </div>
    );
}

export default App;
