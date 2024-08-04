import { AccountCircle, Visibility, VisibilityOff } from '@mui/icons-material';
import { Box, Button, Container, FormHelperText, IconButton, InputAdornment, TextField, Typography } from '@mui/material'
import React, { useState } from 'react'
import LockIcon from '@mui/icons-material/Lock';
import { Formik, useFormik } from 'formik';
function Login() {
    const [showPassword, setShowPassword] = useState(false);

    const validate = values => {
        const errors = {};
        if (!values.email) {
            errors.email = 'Required';
        } else if (!/^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$/i.test(values.email)) {
            errors.email = 'Invalid email address';
        }

        return errors;
    };
    const formik = useFormik({
        initialValues: {
            email: "",
            password: ""
        },
        validate,
        onSubmit: values => {
            alert(JSON.stringify(values, null, 2))
        }
    });
    const INPUT_CSS = {
        "& label": { color: "black" },
        "& input": { color: "black" },
        "& label.Mui-focused": { color: "black" },
        "& .MuiOutlinedInput-root": {
            "& fieldset": { borderColor: "black" },
            "&:hover fieldset": { borderColor: "black" },
            "&.Mui-focused fieldset": { borderColor: "black" }
        }
    }

    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };

    const handleClickShowPassword = () => setShowPassword((show) => !show);
    return (
        <Container maxWidth={false} disableGutters sx={{ bgcolor: "primary.main", height: "100vh", pt: '150px' }}>
            <Box sx={{
                width: "400px",
                margin: "auto",
                textAlign: "center",
                border: "1px solid white",
                padding: "30px",
                borderRadius: "10px",
                bgcolor: "white"
            }}>
                <Typography align='center' variant='h4' sx={{ fontWeight: "bold" }}>
                    LOGIN
                </Typography>
                <form onSubmit={formik.handleSubmit}>
                    <TextField id="email" label="Email" name='email' variant="outlined" fullWidth sx={INPUT_CSS}
                        onChange={formik.handleChange}
                        value={formik.values.email}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <AccountCircle />
                                </InputAdornment>
                            ),
                        }}
                    />
                    {formik.errors.email ? <Typography sx={{
                        color: "error.main", fontSize: "12px",
                        textAlign: "left"
                    }}>{formik.errors.email}</Typography> : null}
                    <br></br>
                    <TextField
                        id="password"
                        label="Password"
                        variant="outlined"
                        name='password'

                        type={showPassword ? 'text' : 'password'}
                        fullWidth
                        sx={{ ...INPUT_CSS, marginTop: "15px" }}
                        onChange={formik.handleChange}
                        value={formik.values.password}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <LockIcon />
                                </InputAdornment>
                            ),
                            endAdornment: (
                                <InputAdornment position="end">
                                    <IconButton
                                        aria-label="toggle password visibility"
                                        onClick={handleClickShowPassword}
                                        onMouseDown={handleMouseDownPassword}
                                    >
                                        {showPassword ? <VisibilityOff /> : <Visibility />}
                                    </IconButton>
                                </InputAdornment>
                            )
                        }}
                    />

                    <Button variant="contained"
                        sx={{ mt: "20px" }}
                        fullWidth
                        type='submit'
                    >LOGIN</Button>
                </form>
            </Box>
        </Container >
    )
}

export default Login
