import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";
import { useEffect } from "react";
import 'react-image-crop/dist/ReactCrop.css';
import { useDispatch } from "react-redux";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import ClientLayout from "./layout/ClientLayout";
import TutorLayout from "./layout/TutorLayout";
import services from "./plugins/services";
import { setTutorInformation } from "./redux/features/tutorSlice";
import { setUserInformation } from "./redux/features/userSlice";
import { adminRoutes, publicRoutes, tutorRoutes, UnLayoutRoutes } from "./routes/Routes";
import PAGES from "./utils/pages";
import { SignalRProvider } from "./Context/SignalRContext";
import { setAdminInformation } from "./redux/features/adminSlice";
import PaymentPackageManagement from "./pages/Tutor/PaymentPackageManagement/PaymentPackageManagement";
function App() {
  const dispatch = useDispatch();
  useEffect(() => {
    const accessToken = Cookies.get("access_token");
    if (!accessToken) {
      return;
    }
    const decodedToken = jwtDecode(accessToken);
    const userId = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    const role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    if (userId) {
      getUserInformation(userId, role);
    }
  }, [])

  const getUserInformation = async (userId, role) => {
    try {
      services.UserManagementAPI.getUserById(userId, (res) => {
        if (role === "Parent") {
          dispatch(setUserInformation(res.result))
          dispatch(setAdminInformation(undefined))
          dispatch(setTutorInformation(undefined))
        } else if (role === "Tutor") {
          dispatch(setTutorInformation(res.result))
          dispatch(setAdminInformation(undefined))
          dispatch(setUserInformation(undefined))
        } else if (role === "Admin" || role === "Staff" || role === "Manager") {
          dispatch(setAdminInformation(res.result))
          dispatch(setUserInformation(undefined))
          dispatch(setTutorInformation(undefined))
        }
      }, (error) => {
        console.log(error);
      }
      )
    } catch (error) {
      console.log(error);
    }
  };

  return (
    <>
      <SignalRProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Navigate to={PAGES.ROOT + PAGES.HOME} />} />
            {UnLayoutRoutes.map((route) => (
              <Route
                key={route.path}
                path={route.path}
                element={<route.element />}
              />
            ))}

            <Route path="/autismedu" element={<ClientLayout />}>
              {publicRoutes.map((route) => (
                <Route
                  key={route.path}
                  path={route.path}
                  element={<route.element />}
                />
              ))}
            </Route>
            <Route path="/autismtutor/payment-package-focus" element={<PaymentPackageManagement />} />
            <Route path="/autismtutor" element={<TutorLayout />}>
              {tutorRoutes.map((route) => (
                <Route
                  key={route.path}
                  path={route.path}
                  element={<route.element />}
                />
              ))}
            </Route>
            <Route path="/admin" element={<AdminLayout />}>
              {adminRoutes.map((route) => (
                <Route
                  key={route.path}
                  path={route.path}
                  element={<route.element />}
                />
              ))}
            </Route>
          </Routes>
        </BrowserRouter>
      </SignalRProvider>
    </>
  )
}

export default App
