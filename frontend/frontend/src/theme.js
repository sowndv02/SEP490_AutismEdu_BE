import { createTheme } from '@mui/material/styles';
import { red } from '@mui/material/colors';

// Create a theme instance.
const labelFontSize = "12px";
const theme = createTheme({
    myapp: {
        adminHeaderHeight: '70px',
        adminSideBarWidth: '260px'
    },
    palette: {
        primary: {
            main: '#556cd6'
        },
        secondary: {
            main: '#19857b'
        },
        error: {
            main: red.A400
        },
        text: {
            primary: '#676b7b',
            secondary: '#3b4056'
        }
    },

    typography: {
        h1: {
            fontWeight: 'bold',
            color: '#192335'
        },
        h2: {
            fontWeight: 'bold',
            color: '#192335'
        },
        h3: {
            fontWeight: 'bold',
            color: '#192335'
        },
        h4: {
            fontWeight: 'bold',
            color: '#192335'
        },
        h5: {
            fontWeight: 'bold',
            color: '#192335'
        },
        h6: {
            fontWeight: 'bold',
            color: '#192335'
        },
    },
    components: {
        MuiCssBaseline: {
            styleOverrides: {
                a: {
                    color: '#3b4056',
                    textDecoration: 'none',
                },
                'h1, h2, h3, h4, h5, h6': {
                    fontWeight: 'bold',
                    color: '#192335'
                },
            }
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    textTransform: "none"
                }
            }
        }
    }
});

export default theme;