import { createContext } from "react";

export type AuthStatus =
    | "loading"
    | "authenticated"
    | "unauthenticated"
    | "error";

export type AuthUser = {
    id: string;
    email: string;
};

export type AuthContextValue = {
    status: AuthStatus;
    user: AuthUser | null;
    login: (email: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
    errorMessage : string | null
};

export const AuthContext = createContext<AuthContextValue | undefined>(
    
    undefined
);