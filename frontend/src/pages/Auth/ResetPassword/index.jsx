import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import { Box, Divider, FormControl, FormHelperText, IconButton, InputAdornment, InputLabel, OutlinedInput, SvgIcon, TextField } from '@mui/material';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Typography from '@mui/material/Typography';
import { useState } from 'react';
import TrelloIcon from '~/assets/trello.svg?react';
import GoogleIcon from '@mui/icons-material/Google';
import { Link, useLocation } from 'react-router-dom';
import PAGES from '~/utils/pages';
import service from '~/plugins/services'
import ArrowBackIosNewIcon from '@mui/icons-material/ArrowBackIosNew';
function ResetPassword() {
    const [showPassword, setShowPassword] = useState(false);
    const [passwordError, setPasswordError] = useState(null);
    const [passwordConfirmError, setPasswordConfirmError] = useState(null);
    const [password, setPassword] = useState("");
    const [cfPassword, setCfPassword] = useState("");
    const location = useLocation();
    const urlParams = new URLSearchParams(location.search);
    const userId = urlParams.get('userId');
    const code = urlParams.get('code');

    const INPUT_CSS = {
        width: "100%",
        borderRadius: "15px"
    };

    const handleMouseDownPassword = (event) => {
        event.preventDefault();
    };
    const handleClickShowPassword = () => setShowPassword((show) => !show);

    const checkValid = (value, field) => {
        if (field === 1) {
            if (value === "") {
                console.log("zoday", password);
                setPasswordError("Please enter password");
            } else if (value.length < 6) {
                setPasswordError("Username must be more than 6 characters")
            } else {
                setPasswordError(null)
            }
        }
        if (field === 2) {
            if (value === "") {
                setPasswordConfirmError("Please enter confirm password")
            } else if (value.length < 6) {
                setPasswordConfirmError("Password must be more than 6 characters")
            } else {
                setPasswordConfirmError(null)
            }
        }
    }
    const handleSubmit = () => {
        checkValid();
        // service.AuthenticationAPI.getData({}, (res) => {
        //     console.log("data", res);
        // }, (err) => {
        //     console.log(err);
        // })
    }
    return (
        <Box sx={{ bgcolor: "#f7f7f9", height: "100vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
            <Card sx={{
                width: "450px",
                boxShadow: "rgba(0, 0, 0, 0.35) 0px 5px 15px",
                borderRadius: "10px",
                p: "28px"
            }}>
                <CardContent>
                    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", gap: 1 }}>
                        <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
                        <Typography sx={{ fontSize: 20, fontWeight: "bold", color: "text.secondary" }}>
                            My App
                        </Typography>
                    </Box>
                    <Typography variant='h5' sx={{ color: "text.secondary", mt: "20px" }}>Reset Password 🔒</Typography>
                    <Typography sx={{ mt: "10px" }}>Your new password must be different from previously used passwords</Typography>
                    <Box mt="30px">
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="new-password">New Password</InputLabel>
                            <OutlinedInput
                                error={!!passwordError}
                                value={password}
                                id="new-password"
                                type={showPassword ? 'text' : 'password'}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        setPassword(e.target.value);
                                        checkValid(e.target.value, 1);
                                    }
                                }}
                                endAdornment={
                                    <InputAdornment position="end">
                                        <IconButton
                                            aria-label="toggle password visibility"
                                            onClick={handleClickShowPassword}
                                            onMouseDown={handleMouseDownPassword}
                                            edge="end"
                                        >
                                            {showPassword ? <VisibilityOff /> : <Visibility />}
                                        </IconButton>
                                    </InputAdornment>
                                }
                                label="Password"
                            />
                            {
                                passwordError && (
                                    <FormHelperText error id="password-error">
                                        {passwordError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                        <FormControl sx={{ ...INPUT_CSS, mt: "20px" }} variant="outlined">
                            <InputLabel htmlFor="confirm-password">Confirm Password</InputLabel>
                            <OutlinedInput
                                error={!!passwordConfirmError}
                                value={cfPassword}
                                id="confirm-password"
                                type={showPassword ? 'text' : 'password'}
                                onChange={(e) => {
                                    if (!e.target.value.includes(" ")) {
                                        setCfPassword(e.target.value);
                                        checkValid(e.target.value, 2);
                                    }
                                }}
                                endAdornment={
                                    <InputAdornment position="end">
                                        <IconButton
                                            aria-label="toggle password visibility"
                                            onClick={handleClickShowPassword}
                                            onMouseDown={handleMouseDownPassword}
                                            edge="end"
                                        >
                                            {showPassword ? <VisibilityOff /> : <Visibility />}
                                        </IconButton>
                                    </InputAdornment>
                                }
                                label="Password"
                            />
                            {
                                passwordConfirmError && (
                                    <FormHelperText error id="cf-password-id">
                                        {passwordConfirmError}
                                    </FormHelperText>
                                )
                            }
                        </FormControl>
                    </Box>
                    <Button variant='contained' sx={{ width: "100%", marginTop: "20px" }} onClick={handleSubmit}>Set New Password</Button>
                    <Typography textAlign={'center'} mt="20px">
                        <Link to={PAGES.LOGIN} style={{ color: "#666cff" }}>
                            <ArrowBackIosNewIcon sx={{ fontSize: "12px" }} /> Back to login
                        </Link>
                    </Typography>

                </CardContent>
            </Card>
        </Box>
    );
}

export default ResetPassword;
