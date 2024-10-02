import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import { adminRoutes, publicRoutes } from "./routes/Routes";
import ClientLayout from "./layout/ClientLayout";
import PAGES from "./utils/pages";
import 'react-image-crop/dist/ReactCrop.css'
import { useEffect } from "react";
import services from "./plugins/services";
import Cookies from "js-cookie";
import { jwtDecode } from "jwt-decode";
import { enqueueSnackbar } from "notistack";
import { setUserInformation } from "./redux/features/userSlice";
import { useDispatch } from "react-redux";
function App() {
  const dispatch = useDispatch();
  useEffect(() => {
    const accessToken = Cookies.get("access_token");
    const decodedToken = jwtDecode(accessToken);
    const userId = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    if (userId) {
      getUserInformation(userId);
    }
    // services.UserManagementAPI.get
  }, [])

  const getUserInformation = async (userId) => {
    try {
      services.UserManagementAPI.getUserById(userId, (res) => {
        dispatch(setUserInformation(res.result))
      }, (error) => {
        console.log(error);
      }
      )
    } catch (error) {
      console.log(error);
    }
  }
  return (
    <>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to={PAGES.ROOT + PAGES.HOME} />} />
          <Route path="/autismedu" element={<ClientLayout />}>
            {publicRoutes.map((route) => (
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
    </>
  )
}

export default App
