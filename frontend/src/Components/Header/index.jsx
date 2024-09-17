import { Badge, Box, Button, IconButton, Paper, Stack, Tab, Tabs, Typography } from '@mui/material'
import React, { useState } from 'react'
import Logo from '../Logo'
import SearchIcon from '@mui/icons-material/Search';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import ButtonComponent from '../ButtonComponent';
import NavigationMobile from './NavigationMobile';
function Header() {
    const [tab, setTab] = useState("1");
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
            <Tabs value={tab} sx={{
                display: {
                    lg: "block",
                    xs: "none"
                }
            }}>
                <Tab value={"1"} label="Trang chủ" />
                <Tab value={"2"} label="Trung tâm" />
                <Tab value={"3"} label="Gia sư" />
                <Tab value={"4"} label="Lớp học" />
                <Tab value={"5"} label="Blog" />
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
