import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import useAuth from "../../auth/useAuth";

export default function LoginPage() {

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const { login } = useAuth();

    const navigate = useNavigate();


    async function handleLogin(): Promise<void> {
        setError("");
        try {
            await login(email, password)
            navigate("/main-menu");
        } catch (err) {
            setError((err as Error).message);
        }
    }

    return (

        <div className="loginPage">
            <h1>Logowanie</h1>
            <div className="modal-body">
                <input
                    type="text"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />
                <input
                    type="password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                {error && <p>{error}</p>}
                <button className="btn btn-primary" onClick={handleLogin}>
                    Zaloguj się
                </button>
                <div className="auth-switch">
                    Nie masz konta? <Link to="/register">Zarejestruj się</Link>
                </div>
            </div>
        </div>
    )
}