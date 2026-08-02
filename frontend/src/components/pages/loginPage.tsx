import { useState } from "react";
import { useNavigate } from "react-router-dom";


type LoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
};
export default function LoginPage() {

    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    async function login(): Promise<void> {
        setError(""); 

        try {
        const response = await fetch("http://localhost:5235/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                email: email,
                password: password,
            }),
        });        
        if (response.status === 401) {
            setError("Nieprawidłowy email lub hasło.");
            return;
        }

        if (!response.ok) {
            setError("Wystąpił błąd. Spróbuj ponownie.");
            return;
        }

        const data : LoginResponse = await response.json();
        localStorage.setItem("accessToken", data.accessToken);
        localStorage.setItem("expiresAtUtc", data.expiresAtUtc);
        navigate("/main-menu");
    }
    catch {
        setError("Wystąpił błąd. Spróbuj ponownie.");
    }}

    return (

        <div className="loginPage">
            <h1>Login Page</h1>
            <div className="modal-body">
                <input
                    type="text"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />
                <input
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                {error && <p>{error}</p>}
                <button className="btn btn-primary" onClick={() => login()}>
                    Login
                </button>
            </div>
        </div>
    )
}