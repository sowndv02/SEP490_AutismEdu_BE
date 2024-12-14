import MenuIcon from '@mui/icons-material/Menu';
import { Avatar, Button, IconButton, Typography } from '@mui/material';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import React from 'react';
import SchoolIcon from '@mui/icons-material/School';
import PersonSearchIcon from '@mui/icons-material/PersonSearch';
import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import NewspaperIcon from '@mui/icons-material/Newspaper';
import LogoutIcon from '@mui/icons-material/Logout';
import ButtonComponent from '~/components/ButtonComponent';
function NavigationMobile() {
    const [open, setOpen] = React.useState(false);

    const toggleDrawer = (newOpen) => () => {
        setOpen(newOpen);
    };
    const DrawerList = (
        <Box sx={{ width: "100%" }} role="presentation" onClick={toggleDrawer(false)}>
            <List>
                <ListItem>
                    <Avatar alt="Remy Sharp"
                        src="https://scontent.fhan18-1.fna.fbcdn.net/v/t39.30808-1/268142468_3035907700072578_4829229204736514171_n.jpg?stp=cp0_dst-jpg_s40x40&_nc_cat=100&ccb=1-7&_nc_sid=0ecb9b&_nc_eui2=AeFe_w7HSGpqFDepgviEP4pyq9KSuRzAWe6r0pK5HMBZ7pEuCwmHx3H-gP4TXxRF640CJIZj8zT62i8cDsbhFZrr&_nc_ohc=bFMv_CKAR0wQ7kNvgGq3_lK&_nc_ht=scontent.fhan18-1.fna&_nc_gid=AiytevtAFd4ZH98szlnXKTJ&oh=00_AYBMrxBp510Ipn2XUHfEL46voYJqbqJF8zkwIGoQMaJLZg&oe=66EB5605" />
                    <Box ml={2}>
                        <Typography>Đào Quang Khair</Typography>
                        <Typography sx={{ fontSize: "13px" }}>daoquangkhai2002@gmail.com</Typography>
                    </Box>
                </ListItem>
                <ListItem>
                    <ListItemButton sx={{ ml: "40px" }}>
                        <ListItemText>Xem trang cá nhân</ListItemText>
                    </ListItemButton>
                </ListItem>
            </List>
            <Divider />
            <List>
                <ListItem disablePadding>
                    <ListItemButton>
                        <ListItemIcon>
                            <SchoolIcon />
                        </ListItemIcon>
                        <ListItemText primary={"Trung tâm"} />
                    </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                    <ListItemButton>
                        <ListItemIcon>
                            <PersonSearchIcon />
                        </ListItemIcon>
                        <ListItemText primary={"Gia sư"} />
                    </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                    <ListItemButton>
                        <ListItemIcon>
                            <MeetingRoomIcon />
                        </ListItemIcon>
                        <ListItemText primary={"Lớp"} />
                    </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                    <ListItemButton>
                        <ListItemIcon>
                            <NewspaperIcon />
                        </ListItemIcon>
                        <ListItemText primary={"Blog"} />
                    </ListItemButton>
                </ListItem>
                <ListItem disablePadding>
                    <ListItemButton>
                        <ListItemIcon>
                            <LogoutIcon />
                        </ListItemIcon>
                        <ListItemText primary={"Đăng xuất"} />
                    </ListItemButton>
                </ListItem>
            </List>
            <Box sx={{ display: "flex", gap: 2, justifyContent: "center" }}>
                <ButtonComponent text="Đăng nhập" height="40px" />
                <Button variant='outlined' >Đăng ký</Button>
            </Box>
        </Box>
    );

    return (
        <div>
            <IconButton onClick={toggleDrawer(true)} sx={{
                display: {
                    md: "block",
                    lg: "none"
                }
            }}><MenuIcon /></IconButton>
            <Drawer open={open} onClose={toggleDrawer(false)} sx={{
                ".MuiDrawer-paper": {
                    width: "70% !important"
                }
            }}>
                {DrawerList}
            </Drawer>
        </div>
    )
}

export default NavigationMobile
