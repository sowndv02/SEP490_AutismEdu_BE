import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined';
import ContactPageOutlinedIcon from '@mui/icons-material/ContactPageOutlined';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import GroupOutlinedIcon from '@mui/icons-material/GroupOutlined';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined';
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import { Avatar, Divider, Drawer } from '@mui/material';
import Collapse from '@mui/material/Collapse';
import List from '@mui/material/List';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import * as React from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Link, useLocation } from 'react-router-dom';
import services from '~/plugins/services';
import { listStudent, setListStudent } from '~/redux/features/listStudent';
import PAGES from '~/utils/pages';
export default function TutorSideBar({ openMenu }) {
    const [openStudent, setOpenStudent] = React.useState(true);
    const [selectedIndex, setSelectedIndex] = React.useState(0);
    const [currentStudent, setCurrentStudent] = React.useState(0)
    const listStudents = useSelector(listStudent);
    const dispatch = useDispatch();
    const location = useLocation();
    React.useEffect(() => {
        getListStudent();
    }, []);
    React.useEffect(() => {
        if (location.pathname === PAGES.MY_STUDENT) {
            setSelectedIndex(0);
        }
        else if (location.pathname.includes(PAGES.CALENDAR)) {
            setSelectedIndex(1);
        }
        else if (location.pathname.includes(PAGES.TUTOR_REQUEST)) {
            setSelectedIndex(2);
        }
        else if (location.pathname.includes(PAGES.STUDENT_CREATION)) {
            setSelectedIndex(3);
        }
        else if (location.pathname.includes('/autismtutor/exercise')) {
            setSelectedIndex(5);
        }
        else if (location.pathname.includes(PAGES.TUTOR_SETTING)) {
            setSelectedIndex(6);
        }
        else if (location.pathname.includes(PAGES.ASSESSMENT_GUILD)) {
            setSelectedIndex(7);
        }
    }, [location]);

    const getListStudent = async () => {
        try {
            await services.StudentProfileAPI.getListStudent((res) => {
                dispatch(setListStudent(res.result))
            }, (error) => {
                console.log(error);
            }, {
                status: "Teaching"
            })
        } catch (error) {
            console.log(error);
        }
    };

    const handleListItemClick = (event, index) => {
        setSelectedIndex(index);
    };
    const textStyle = {
        opacity: openMenu ? 1 : 0,
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        transition: 'opacity 0.3s'
    }

    const listIconStyle = {
        minWidth: openMenu ? '56px' : '40px',
        justifyContent: "center",
        transition: 'min-width 0.3s ease'
    }
    const handleOpenStudent = () => {
        setOpenStudent(!openStudent);
    };
    return (
        <Drawer variant="permanent" open={openMenu} sx={{
            width: openMenu ? 300 : 80,
            flexShrink: 0,
            '& .MuiDrawer-paper': {
                width: openMenu ? 300 : 80,
                top: 65,
                height: `calc(100vh - 64px)`,
                transition: 'width 0.3s'
            },
        }}>
            <List
                sx={{
                    width: `${openMenu ? "100%" : "76px"}`,
                    maxWidth: 300,
                    height: "calc(100vh - 65px)",
                    bgcolor: 'background.paper'
                }}
                aria-labelledby="nested-list-subheader"
            >
                <Link to={PAGES.MY_STUDENT}>
                    <ListItemButton
                        selected={selectedIndex === 0}
                        onClick={(event) => handleListItemClick(event, 0)}
                    >
                        <ListItemIcon sx={listIconStyle}>
                            <HomeOutlinedIcon sx={{ color: "#4CAFEB" }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Trang chủ"
                            sx={textStyle}
                        />
                    </ListItemButton>
                </Link>
                <Link to={PAGES.CALENDAR}>
                    <ListItemButton
                        selected={selectedIndex === 1}
                        onClick={(event) => handleListItemClick(event, 1)}
                    >
                        <ListItemIcon sx={listIconStyle}>
                            <CalendarMonthOutlinedIcon sx={{ color: "#4CAF50" }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Lịch dạy"
                            sx={textStyle}
                        />
                    </ListItemButton>
                </Link>
                <Link to={PAGES.TUTOR_REQUEST}>
                    <ListItemButton
                        selected={selectedIndex === 2}
                        onClick={(event) => handleListItemClick(event, 2)}
                    >
                        <ListItemIcon sx={listIconStyle}>
                            <ContactPageOutlinedIcon sx={{ color: "#FF9800" }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Yêu cầu dạy"
                            sx={textStyle}
                        />
                    </ListItemButton>
                </Link>
                <Divider />
                <Link to={PAGES.STUDENT_CREATION}>
                    <ListItemButton
                        selected={selectedIndex === 3}
                        onClick={(event) => handleListItemClick(event, 3)}
                    >
                        <ListItemIcon sx={listIconStyle}>
                            <PersonAddIcon sx={{ color: "#9C27B0" }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Tạo hồ sơ học sinh"
                            sx={textStyle}
                        />
                    </ListItemButton>
                </Link>
                <ListItemButton onClick={handleOpenStudent}
                    selected={selectedIndex === 4}
                >
                    <ListItemIcon sx={listIconStyle}>
                        <GroupOutlinedIcon sx={{ color: "#E91E63" }} />
                    </ListItemIcon>
                    <ListItemText primary="Học sinh" style={textStyle} />
                    {openMenu && (openStudent ? <ExpandLess /> : <ExpandMore />)}
                </ListItemButton>
                <Collapse in={openStudent && openMenu} timeout="auto" unmountOnExit>
                    {
                        listStudents && listStudents.length !== 0 && listStudents.map((l, index) => {
                            return (
                                <Link to={'/autismtutor/student-detail/' + l.id} key={l.id}>
                                    <List component="div" disablePadding>
                                        <ListItemButton
                                            onClick={(event) => { setCurrentStudent(index); handleListItemClick(event, 4) }}
                                            selected={selectedIndex === 4 && currentStudent === index}
                                        >
                                            <ListItemIcon sx={listIconStyle}>
                                                <Avatar alt={l.studentCode} src={l?.imageUrlPath || '/'} sx={{ background: "black", width: "30px", height: "30px" }} />
                                            </ListItemIcon>
                                            <ListItemText primary={`${l.studentCode} - ${l.name}`} />
                                        </ListItemButton>
                                    </List>
                                </Link>
                            )
                        })
                    }
                </Collapse>
                <Divider />
                <Link to={PAGES.EXERCISE_MANAGEMENT}>
                    <ListItemButton selected={selectedIndex === 5}
                        onClick={(event) => handleListItemClick(event, 5)}
                    >
                        <ListItemIcon sx={listIconStyle}>
                            <MenuBookOutlinedIcon sx={{ color: "#FFA726" }} />
                        </ListItemIcon>
                        <ListItemText primary="Bài tập" style={textStyle} />
                    </ListItemButton>
                </Link>

                <Divider />
                <Link to={PAGES.TUTOR_SETTING} >
                    <ListItemButton selected={selectedIndex === 6}
                        onClick={(event) => handleListItemClick(event, 6)}>
                        <ListItemIcon sx={listIconStyle}>
                            <SettingsOutlinedIcon sx={{ color: "#607D8B" }} />
                        </ListItemIcon>
                        <ListItemText primary="Cài đặt" style={textStyle} />
                    </ListItemButton>
                </Link>
                <Link to={PAGES.ASSESSMENT_GUILD} >
                    <ListItemButton selected={selectedIndex === 7}
                        onClick={(event) => handleListItemClick(event, 7)}>
                        <ListItemIcon sx={listIconStyle}>
                            <HelpOutlineIcon sx={{ color: "#3F51B5" }} />
                        </ListItemIcon>
                        <ListItemText primary="Tiêu chí đánh giá" style={textStyle} />
                    </ListItemButton>
                </Link>
            </List>
        </Drawer>
    );
}
