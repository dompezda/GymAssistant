import { createContext } from "react";

export type AuthStatus =
    | "loading"
    | "authenticated"
    | "unauthenticated";

export type AuthUser = {
    id: string;
    email: string;
};

export type AuthContextValue = {
    status: AuthStatus;
    user: AuthUser | null;
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | undefined>(
    
    undefined
);