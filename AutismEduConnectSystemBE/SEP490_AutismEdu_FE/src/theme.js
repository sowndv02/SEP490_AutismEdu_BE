import { createTheme } from '@mui/material/styles';
import { red } from '@mui/material/colors';
import './assets/css/EuclidCircularARegular.ttf'
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
    breakpoints: {
        values: {
            xs: 0,
            sm: 700,
            md: 900,
            lg: 1200,
            xl: 1536,
        },
    },
    typography: {
        fontFamily: 'Euclid Circular, Arial, sans-serif',
        fontSize: 14,
        button: {
            textTransform: 'none',
        },
        h1: {
            fontSize: "36px",
            fontWeight: '700',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "36px"
            },
            '@media (min-width:900px)': {
                fontSize: "52px"
            }
        },
        h2: {
            fontSize: "30px",
            fontWeight: '700',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "28px"
            },
            '@media (min-width:900px)': {
                fontSize: "44px"
            }
        },
        h3: {
            fontSize: "26px",
            fontWeight: '700',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "24px"
            },
            '@media (min-width:900px)': {
                fontSize: "34px"
            }
        },
        h4: {
            fontSize: "24px",
            fontWeight: '700',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "20"
            },
            '@media (min-width:900px)': {
                fontSize: "30px"
            }
        },
        h5: {
            fontSize: "20px",
            fontWeight: '700',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "16px"
            },
            '@media (min-width:900px)': {
                fontSize: "20px"
            }
        },
        h6: {
            fontSize: "18px",
            fontWeight: '600',
            color: '#192335',
            '@media (min-width:600px)': {
                fontSize: "13px"
            },
            '@media (min-width:900px)': {
                fontSize: "16px"
            }
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
                }
            }
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    textTransform: "none"
                }
            }
        },
        MuiBackdrop: {
            styleOverrides: {
                root: {
                    zIndex: 1301
                }
            }
        }
    }
});

export default theme;