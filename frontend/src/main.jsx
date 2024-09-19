import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.jsx'
import './index.css'
import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles';
import theme from './theme';
import { SnackbarProvider } from 'notistack'
import { GoogleOAuthProvider } from '@react-oauth/google';
import { Provider } from 'react-redux'
import store from './redux/app/store.js';
ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Provider store={store}>
<<<<<<< HEAD
        <GoogleOAuthProvider clientId="284134545636-8tqdps21ukl8494tqu7ean9fn5o4s9tk.apps.googleusercontent.com">
=======
        <GoogleOAuthProvider clientId={import.meta.env.VITE_CLIENT_ID}>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
          <SnackbarProvider anchorOrigin={{
            vertical: 'top',
            horizontal: 'right'
          }}
            autoHideDuration={4000}>
            <App />
          </SnackbarProvider>
        </GoogleOAuthProvider>
      </Provider>
    </ThemeProvider>
  </React.StrictMode>
)
