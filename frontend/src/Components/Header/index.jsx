import { Badge, Box, Button, IconButton, Paper, Stack, Tab, Tabs, Typography } from '@mui/material'
import React from 'react'
import Logo from '../Logo'
import SearchIcon from '@mui/icons-material/Search';
import BookmarkBorderIcon from '@mui/icons-material/BookmarkBorder';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
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
            <Tabs aria-label="nav tabs">
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

                <Button variant='contained'>Đăng nhập</Button>
                <Button variant='outlined'>Đăng ký</Button>
            </Stack>
        </Stack>
    )
}

export default Header
