<<<<<<< HEAD
import { BrowserRouter, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import { adminRoutes, publicRoutes } from "./routes/Routes";
import ClientLayout from "./layout/ClientLayout";
=======
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import AdminLayout from "./layout/AdminLayout";
import { adminRoutes, publicRoutes } from "./routes/Routes";
import ClientLayout from "./layout/ClientLayout";
import PAGES from "./utils/pages";
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f

function App() {
  return (
    <>
      <BrowserRouter>
        <Routes>
<<<<<<< HEAD
=======
          <Route path="/" element={<Navigate to={PAGES.ROOT + PAGES.HOME} />} />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
          <Route path="/autismedu" element={<ClientLayout />}>
            {publicRoutes.map((route) => (
              <Route
                key={route.path}
<<<<<<< HEAD
                path={`/autismedu/${route.path}`}
=======
                path={route.path}
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
