import { useEffect, useState, type ReactNode } from "react";
import {
    AuthContext,
    type AuthContextValue,
    type AuthStatus,
    type AuthUser,
} from "./authContext";
import { fetchApi } from "../api/fetchApi";

type LoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
};
type AuthProviderProps = {
    children: ReactNode;
};

export default function AuthProvider({ children }: AuthProviderProps) {

const [status, setStatus] = useState<AuthStatus>("loading");
const [user, setUser] = useState<AuthUser | null>(null);
const [errorMessage, setErrorMessage] = useState<string | null>(null);
const authContextValue: AuthContextValue = {
    status,
    user,
    login: login,
    logout: logout,
    errorMessage: errorMessage
};
useEffect(() => {
    console.log("Auth state changed:", status, user);
}, [status, user]);


    async function logout() : Promise<void>
    {
        try
        {
            await fetch("http://localhost:5235/api/auth/logout", {
            method: "POST",
            credentials: "include"
        }); 
        }
        finally
        {
            localStorage.removeItem("accessToken");
            localStorage.removeItem("expiresAtUtc");
            setUser(null);
            setErrorMessage(null);
            setStatus("unauthenticated");
        }
    }

    async function login(email: string, password: string):Promise<void> {
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
        credentials: "include"
    });        
    if (response.status === 401) {
        throw new Error("Nieprawidłowy email lub hasło");
    }

    if (!response.ok) {
        throw new Error("Wystąpił błąd. Spróbuj ponownie.");
    }

    const data : LoginResponse = await response.json();
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("expiresAtUtc", data.expiresAtUtc);


    const authMeRequest = await fetch("http://localhost:5235/api/auth/me", {
        headers: {
            Authorization: `Bearer ${data.accessToken}`,
        },
    });
    if(!authMeRequest.ok)
    {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("expiresAtUtc");
        throw new Error("Wystąpił błąd. Spróbuj ponownie.");
    } 
    const userData: AuthUser = await authMeRequest.json();
    setUser(userData);
    setStatus("authenticated");
}
catch(err) {
    setStatus("unauthenticated");
    setUser(null);
    localStorage.removeItem("accessToken");
    localStorage.removeItem("expiresAtUtc");
    throw err;
}}


useEffect(() => {

    async function checkAuthentication() {
        try
        {
            const response = await fetchApi("http://localhost:5235/api/auth/me", {
            });
            if(response.status === 401)
            {
                setUser(null);
                setStatus("unauthenticated");
                setErrorMessage(null);
                return;
            }
            if(!response.ok)
            {
                setStatus("error");
                setErrorMessage(`Błąd odpowiedzi ${response.status}`);
                return;
            }
            const userData: AuthUser = await response.json();
            setUser(userData);
            setErrorMessage(null);
            setStatus("authenticated");
        }
        catch
        {
            setErrorMessage("Nie udało się połączyć z serwerem");
            setStatus("error");
        }      
    }
    checkAuthentication();
},[]);

return (
    <AuthContext.Provider value={authContextValue}>
        {children}
    </AuthContext.Provider>
);
}

