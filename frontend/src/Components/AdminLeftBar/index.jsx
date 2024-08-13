import { Box, Collapse, List, ListItemButton, ListItemIcon, ListItemText, ListSubheader, SvgIcon, Typography } from '@mui/material'
import TrelloIcon from '~/assets/trello.svg?react';
import InboxIcon from '@mui/icons-material/MoveToInbox';
import PeopleIcon from '@mui/icons-material/People';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import StarBorder from '@mui/icons-material/StarBorder';
import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
function AdminLeftBar() {
    const [open, setOpen] = useState(false);
    const location = useLocation();
    const handleClick = () => {
        setOpen(!open);
    };
    const [selectedIndex, setSelectedIndex] = useState(0);

    useEffect(() => {
        if (location.pathname.includes("/dashboard")) {
            setSelectedIndex(0);
        } else if (location.pathname.includes("/user-management")) {
            setSelectedIndex(1);
        }
        else if (location.pathname.includes("/role-claim-management")) {
            setSelectedIndex(2);
        }
    }, [])
    const handleListItemClick = (event, index) => {
        setSelectedIndex(index);
    };

    return (
        <>
            <Box sx={{ bgcolor: "#f7f7f9", width: "260px", height: "100vh", px: "15px", pt: "20px" }}>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <SvgIcon component={TrelloIcon} inheritViewBox sx={{ color: 'blue' }} />
                    <Typography variant='h5' sx={{ color: "text.secondary", fontWeight: "bold" }}>My App</Typography>
                </Box>
                <List
                    sx={{ width: '100%' }}
                    component="nav"
                    aria-labelledby="nested-list-subheader"
                >
                    <Link to="/admin/dashboard">
                        <ListItemButton
                            selected={selectedIndex === 0}
                            onClick={(event) => handleListItemClick(event, 0)}>
                            <ListItemIcon>
                                <DashboardIcon />
                            </ListItemIcon>
                            <ListItemText primary="Dashboard" />
                        </ListItemButton>
                    </Link>
                    <Link to="/admin/user-management">
                        <ListItemButton
                            selected={selectedIndex === 1}
                            onClick={(event) => handleListItemClick(event, 1)}>
                            <ListItemIcon>
                                <PeopleIcon />
                            </ListItemIcon>
                            <ListItemText primary="User Management" />
                        </ListItemButton>
                    </Link>
                    <Link to="/admin/role-claim-management">
                        <ListItemButton
                            selected={selectedIndex === 2}
                            onClick={(event) => handleListItemClick(event, 2)}>
                            <ListItemIcon>
                                <PeopleIcon />
                            </ListItemIcon>
                            <ListItemText primary="Claims & Roles" />
                        </ListItemButton>
                    </Link>
                    <ListItemButton onClick={handleClick}>
                        <ListItemIcon>
                            <InboxIcon />
                        </ListItemIcon>
                        <ListItemText primary="Inbox" />
                        {open ? <ExpandLess /> : <ExpandMore />}
                    </ListItemButton>
                    <Collapse in={open} timeout="auto" unmountOnExit>
                        <List component="div" disablePadding>
                            <ListItemButton sx={{ pl: 4 }}
                                selected={selectedIndex === 2}
                                onClick={(event) => handleListItemClick(event, 2)}>
                                <ListItemIcon>
                                    <StarBorder />
                                </ListItemIcon>
                                <ListItemText primary="Starred" />
                            </ListItemButton>
                        </List>
                    </Collapse>
                </List>
            </Box>
        </>
    )
}

export default AdminLeftBar
