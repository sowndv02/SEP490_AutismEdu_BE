import { Badge, Box, Button, IconButton, Paper, Stack, Tab, Tabs, Typography } from '@mui/material'
import React from 'react'
import Logo from '../Logo'
import SearchIcon from '@mui/icons-material/Search';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import ButtonComponent from '../ButtonComponent';
import NavigationMobile from './NavigationMobile';
function Header() {
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
            <Tabs aria-label="nav tabs" sx={{
                display: {
                    lg: "block",
                    xs: "none"
                }
            }}>
                <Tab label="Trang chủ" />
                <Tab label="Trung tâm" />
                <Tab label="Gia sư" />
                <Tab label="Lớp học" />
                <Tab label="Blog" />
            </Tabs>
            <Stack direction="row" sx={{ alignItems: "center" }} spacing={2}>
                <IconButton>
                    <SearchIcon />
                </IconButton>
                <IconButton>
                    <BookmarkBorderIcon />
                </IconButton>

                <IconButton>
                    <Badge badgeContent={4} color="primary">
                        <NotificationsActiveIcon />
                    </Badge>
                </IconButton>
                <Box sx={{
                    display: {
                        xs: "none",
                        lg: "block"
                    }
                }}>
                    <ButtonComponent text="Đăng nhập" height="40px" />
                </Box>
                <Button variant='outlined' sx={{
                    display: {
                        xs: "none",
                        lg: "block"
                    }
                }}>Đăng ký</Button>
                <NavigationMobile />
            </Stack>
        </Stack>
    )
}

export default Header
