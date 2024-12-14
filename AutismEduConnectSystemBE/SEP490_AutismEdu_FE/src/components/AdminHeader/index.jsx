import { Logout } from '@mui/icons-material';
import AddOutlinedIcon from '@mui/icons-material/AddOutlined';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import { Avatar, Badge, Box, Divider, IconButton, ListItemIcon, Menu, MenuItem, Stack, Typography } from '@mui/material';
import { deepPurple } from '@mui/material/colors';
import Cookies from "js-cookie";
import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { adminInfor, setAdminInformation } from '~/redux/features/adminSlice';
import PAGES from '~/utils/pages';
import Logo from '../Logo';
import { jwtDecode } from 'jwt-decode';
function AdminHeader() {
    const nav = useNavigate();
    const [accountMenu, setAccountMenu] = useState();
    const dispatch = useDispatch();
    const openAccountMenu = Boolean(accountMenu);
    const adminInfo = useSelector(adminInfor);
    useEffect(() => {
        const accessToken = Cookies.get("access_token");
        if (!accessToken) {
            nav(PAGES.LOGIN_ADMIN)
            return;
        }
        const decodedToken = jwtDecode(accessToken);
        const role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        if (role !== "Admin" && role !== "Manager" && role !== "Staff") {
            nav(PAGES.LOGIN_ADMIN)
        }
    }, [adminInfo])
    const handleOpenAccountMenu = (event) => {
        setAccountMenu(event.currentTarget);
    };
    const handleCloseAccountMenu = () => {
        setAccountMenu(null);
    };
    const handleLogout = () => {
        Cookies.remove("access_token");
        Cookies.remove("refresh_token");
        dispatch(setAdminInformation(null));
        nav(PAGES.LOGIN_ADMIN)
    }

    return (
        <Box sx={{
            position: "fixed",
            top: "0",
            width: "100vw",
            zIndex: 100,
            bgcolor: 'white'
        }}>
            <Stack direction='row' sx={{
                justifyContent: "space-between",
                height: "64px",
                alignItems: "center",
                px: "20px"
            }}>
                <Box sx={{ display: "flex", gap: 2 }}>
                    <Logo sizeLogo={30} sizeName={25} />
                </Box>
                <Box sx={{ display: "flex", gap: 2 }} alignItems="center">
                    <Typography>{adminInfo?.fullName}</Typography>
                    <Avatar alt={adminInfo?.fullName || "K"} src={'/'} sx={{
                        bgcolor: deepPurple[500], width: "30px",
                        height: "30px",
                        cursor: "pointer"
                    }} onClick={handleOpenAccountMenu} />

                    <Menu
                        anchorEl={accountMenu}
                        id="account-menu"
                        open={openAccountMenu}
                        onClose={handleCloseAccountMenu}
                        onClick={handleCloseAccountMenu}
                        slotProps={{
                            paper: {
                                elevation: 0,
                                sx: {
                                    overflow: 'visible',
                                    filter: 'drop-shadow(0px 2px 8px rgba(0,0,0,0.32))',
                                    mt: 1.5,
                                    '& .MuiAvatar-root': {
                                        width: 32,
                                        height: 32,
                                        ml: -0.5,
                                        mr: 1,
                                    },
                                    '&::before': {
                                        content: '""',
                                        display: 'block',
                                        position: 'absolute',
                                        top: 0,
                                        right: 14,
                                        width: 10,
                                        height: 10,
                                        bgcolor: 'background.paper',
                                        transform: 'translateY(-50%) rotate(45deg)',
                                        zIndex: 0,
                                    },
                                },
                            },
                        }}
                        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                    >
                        <MenuItem onClick={handleLogout}>
                            <ListItemIcon>
                                <Logout fontSize="small" />
                            </ListItemIcon>
                            Đăng xuất
                        </MenuItem>
                    </Menu>
                </Box>
            </Stack>
            <Divider />
        </Box>
    )
}

export default AdminHeader
