<<<<<<< HEAD
import { Badge, Box, Button, IconButton, Paper, Stack, Tab, Tabs, Typography } from '@mui/material'
import React from 'react'
import Logo from '../Logo'
import SearchIcon from '@mui/icons-material/Search';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
function Header() {
=======
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import Logout from '@mui/icons-material/Logout';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import PersonAdd from '@mui/icons-material/PersonAdd';
import SearchIcon from '@mui/icons-material/Search';
import Settings from '@mui/icons-material/Settings';
import { Avatar, Badge, Box, Button, Divider, IconButton, ListItemIcon, Menu, MenuItem, Stack, Tab, Tabs } from '@mui/material';
import React, { useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { setUserInformation, userInfor } from '~/redux/features/userSlice';
import PAGES from '~/utils/pages';
import ButtonComponent from '../ButtonComponent';
import Logo from '../Logo';
import NavigationMobile from './NavigationMobile';
import Cookies from 'js-cookie';
function Header() {
    const [tab, setTab] = useState("1");
    const [anchorEl, setAnchorEl] = React.useState(null);
    const [accountMenu, setAccountMenu] = React.useState(null);
    const [selectedIndex, setSelectedIndex] = React.useState(1);
    const userInfo = useSelector(userInfor);
    const openAccountMenu = Boolean(accountMenu);
    const dispatch = useDispatch();
    const handleOpenAccountMenu = (event) => {
        setAccountMenu(event.currentTarget);
    };
    const handleCloseAccountMenu = () => {
        setAccountMenu(null);
    };

    const open = Boolean(anchorEl);
    const handleClickListItem = (event) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuItemClick = (event, index) => {
        setSelectedIndex(index);
        setAnchorEl(null);
    };

    const handleClose = () => {
        setAnchorEl(null);
    };

    const handleLogout = () => {
        Cookies.remove("access_token");
        Cookies.remove("refresh_token");
        dispatch(setUserInformation(null))
    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    return (
        <Stack
            direction="row"
            spacing={3}
            sx={{
                justifyContent: "space-between", alignItems: "center", position: "fixed", top: "0px",
                height: "80px", width: "100vw", px: "20px",
                zIndex: "10",
                bgcolor: "white"
            }}>
            <Logo sizeLogo="50px" sizeName="35px" />
<<<<<<< HEAD
            <Tabs aria-label="nav tabs">
                <Tab label="Trang chủ" />
                <Tab label="Trung tâm" />
                <Tab label="Gia sư" />
                <Tab label="Lớp học" />
                <Tab label="Blog" />
            </Tabs>
=======
            <Tabs value={tab} sx={{
                display: {
                    lg: "block",
                    xs: "none"
                }
            }}>
                <Tab value={"1"} label="Trang chủ" />
                <Tab value={"2"} label="Trung tâm" icon={<ExpandMoreIcon />} iconPosition="end" onClick={handleClickListItem} />
                <Tab value={"3"} label="Gia sư" onClick={handleClickListItem} />
                <Tab value={"4"} label="Lớp học" />
                <Tab value={"5"} label="Blog" />
            </Tabs>
            <Menu
                id="lock-menu"
                anchorEl={anchorEl}
                open={open}
                onClose={handleClose}
                MenuListProps={{
                    'aria-labelledby': 'lock-button',
                    role: 'listbox',
                }}
            >
                <MenuItem
                    onClick={(event) => handleMenuItemClick(event)}
                >
                    Danh sách trung tâm
                </MenuItem>
            </Menu>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            <Stack direction="row" sx={{ alignItems: "center" }} spacing={2}>
                <IconButton>
                    <SearchIcon />
                </IconButton>
<<<<<<< HEAD
                <IconButton>
                    <BookmarkBorderIcon />
                </IconButton>
=======
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f

                <IconButton>
                    <Badge badgeContent={4} color="primary">
                        <NotificationsActiveIcon />
                    </Badge>
                </IconButton>
<<<<<<< HEAD

                <Button variant='contained'>Đăng nhập</Button>
                <Button variant='outlined'>Đăng ký</Button>
=======
                {
                    !userInfo ? (
                        <>
                            <Box sx={{
                                display: {
                                    xs: "none",
                                    lg: "block"
                                }
                            }}>
                                <Link to={PAGES.ROOT + PAGES.LOGIN}><ButtonComponent text="Đăng nhập" height="40px" /></Link>
                            </Box>
                            <Link to={PAGES.ROOT + PAGES.REGISTER}><Button variant='outlined' sx={{
                                display: {
                                    xs: "none",
                                    lg: "block"
                                }
                            }}>Đăng ký</Button>
                            </Link>
                        </>
                    ) : (
                        <>
                            <Avatar alt="Remy Sharp" src={userInfo.imageLocalUrl}
                                onClick={handleOpenAccountMenu} />
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
                                <MenuItem onClick={handleCloseAccountMenu}>
                                    <Avatar /> Profile
                                </MenuItem>
                                <MenuItem onClick={handleCloseAccountMenu}>
                                    <Avatar /> My account
                                </MenuItem>
                                <Divider />
                                <MenuItem onClick={handleCloseAccountMenu}>
                                    <ListItemIcon>
                                        <PersonAdd fontSize="small" />
                                    </ListItemIcon>
                                    Add another account
                                </MenuItem>
                                <MenuItem onClick={handleCloseAccountMenu}>
                                    <ListItemIcon>
                                        <Settings fontSize="small" />
                                    </ListItemIcon>
                                    Settings
                                </MenuItem>
                                <MenuItem onClick={handleLogout}>
                                    <ListItemIcon>
                                        <Logout fontSize="small" />
                                    </ListItemIcon>
                                    Logout
                                </MenuItem>
                            </Menu>
                        </>
                    )
                }
                <NavigationMobile />
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            </Stack>
        </Stack>
    )
}

export default Header
