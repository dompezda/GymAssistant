import { Navigate, Outlet } from 'react-router-dom'
import useAuth from './useAuth';

export const ProtectedRoute = () => {
  const auth = useAuth();
  if(auth.status === "loading") {
    return <div>Ładowanie...</div>;
  }
  if(auth.status === "unauthenticated") {
    return <Navigate to="/login" replace/>
  }
  if(auth.status === "error")
  {
    return <div>{auth.errorMessage}</div>
  }
  
return (
    //nawigacja na podstrony, jeśli użytkownik jest zalogowany
    <Outlet/>
  )
}