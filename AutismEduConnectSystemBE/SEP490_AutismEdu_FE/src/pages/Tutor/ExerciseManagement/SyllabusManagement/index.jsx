import React, { useEffect, useState } from 'react';
import { Box, Button, Typography, IconButton, Tabs, Tab, Stack } from '@mui/material';
import { TabContext, TabPanel } from '@mui/lab';
import SyllabusCreation from './SyllabusCreation/SyllabusCreation';
import SyllabusAssign from './SyllabusAssign';
import services from '~/plugins/services';
import DeleteConfirmationModal from './SyllabusModal/DeleteConfirmationModal';
import DeleteIcon from '@mui/icons-material/Delete';
import ElevatorIcon from '@mui/icons-material/Elevator';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import AssignmentIcon from '@mui/icons-material/Assignment';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';
import { useLocation } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { tutorInfor } from '~/redux/features/tutorSlice';
import emptyBook from '~/assets/images/icon/emptybook.gif'

export default function SyllabusManagement() {
    const { tutorProfile } = useSelector(tutorInfor);
    const location = useLocation();
    const [openCreation, setOpenCreation] = useState(() => {
        const syllabus = location.state?.syllabus;
        return !!syllabus;
    });
    const [openAssign, setOpenAssign] = useState(false);
    const [listSyllabus, setListSyllabus] = useState([]);
    const [valueSyllabus, setValueSyllabus] = useState('1');
    const [currentDeleteIndex, setCurrentDeleteIndex] = useState(0);
    const [openDeleteConfirm, setOpenDeleteConfirm] = useState(false);
    const [selectedAssign, setSelectedAssign] = useState(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        handleGetAllSyllabus();
    }, []);

    const handleGetAllSyllabus = async () => {
        try {
            setLoading(true);
            await services.SyllabusManagementAPI.getListSyllabus((res) => {
                if (res?.result) {
                    setListSyllabus(res.result);
                }
            }, (error) => {
                console.log(error);
            }, { orderBy: 'ageFrom', sort: 'asc' });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const handleOpenCreation = () => {
        setOpenCreation(true);
    };

    const handleChangeSyllabus = (event, newValue) => {
        setValueSyllabus(newValue);
    };

    const handleOpenDeleteConfirm = (index) => {
        setCurrentDeleteIndex(index);
        setOpenDeleteConfirm(true);
    };

    const handleAssign = (syllabus) => {
        setSelectedAssign(syllabus);
        setOpenAssign(true);
    }

    const handleDelete = async () => {
        try {
            setLoading(true);
            await services.SyllabusManagementAPI.deleteSyllabus(currentDeleteIndex, {}, (res) => {
                if (res?.statusCode === 204) {
                    const updateListSyllabus = listSyllabus.filter((s) => s.id !== currentDeleteIndex);
                    setListSyllabus(updateListSyllabus);
                    enqueueSnackbar("Xoá giáo trình thành công!", { variant: 'success' });
                    setOpenDeleteConfirm(false);
                }

            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }

    };


    if (openCreation && tutorProfile) {
        return (<SyllabusCreation handleBack={() => setOpenCreation(false)} setListSyllabus={setListSyllabus} tutorProfile={tutorProfile} />);
    }

    if (openAssign && tutorProfile && selectedAssign) {
        return (<SyllabusAssign handleBack={() => setOpenAssign(false)} selectedAssign={selectedAssign} setListSyllabus={setListSyllabus} tutorProfile={tutorProfile} />);
    }


    return (
        <Stack direction='column' sx={{ width: "90%", margin: "auto", gap: 2 }}>
            <Typography variant='h4'>Giáo trình</Typography>
            <Box>
                <Button variant='contained' color='primary' onClick={handleOpenCreation}>Tạo giáo trình</Button>
            </Box>
            {listSyllabus.length !== 0 ?
                <TabContext value={valueSyllabus}>
                    <Box sx={{ maxWidth: { xs: 320, sm: 750 } }} mb={2}>
                        <Tabs
                            value={valueSyllabus}
                            onChange={handleChangeSyllabus}
                            aria-label="icon position tabs example"
                            variant="scrollable"
                            scrollButtons='auto'
                        >
                            {listSyllabus?.map((syllabus, index) => (
                                <Tab
                                    key={index}
                                    value={(index + 1).toString()}
                                    icon={<ElevatorIcon />}
                                    iconPosition="start"
                                    label={(
                                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                            Từ {syllabus.ageFrom} - {syllabus.ageEnd} tuổi

                                            <IconButton
                                                size="small"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    handleOpenDeleteConfirm(syllabus?.id);
                                                }}
                                                color="error"
                                            >
                                                <DeleteIcon />
                                            </IconButton>
                                        </Box>
                                    )}
                                />
                            ))}
                        </Tabs>
                    </Box>
                    {listSyllabus.map((syllabus, index) => (
                        <TabPanel key={index} value={(index + 1).toString()} sx={{ padding: '0' }}>
                            {/* <Box ml={2} dangerouslySetInnerHTML={{ __html: curriculum.description }}></Box> */}
                            <Stack direction={'row'} p={5} borderRadius={3} bgcolor={'#fff8e3'}>
                                <Stack sx={{ width: '80%' }} direction={'column'} gap={2}>
                                    {syllabus?.exerciseTypes?.map((s, index) => (
                                        <Stack direction={'row'} gap={2} sx={{ width: '100%' }} key={index}>
                                            <Box sx={{ width: "5%" }}>
                                                <CheckCircleIcon color='success' fontSize='large' />
                                            </Box>
                                            <Box key={index} sx={{ width: '95%' }} pt={0.5}>
                                                <Typography variant='h5'>{`${index + 1}. `}{s?.exerciseTypeName}</Typography>
                                                <Box ml={2}>
                                                    {s?.exercises?.map((e, index) => (
                                                        <Typography key={index} variant='subtitle1'>{`${index + 1}. `}{e?.exerciseName}</Typography>
                                                    ))}
                                                </Box>
                                            </Box>
                                        </Stack>
                                    ))}
                                </Stack>
                                <Stack direction={'column'} gap={2} justifyContent={'space-between'} alignItems={'center'} sx={{ width: "20%" }}>
                                    <Button variant='contained' startIcon={<AssignmentIcon />} sx={{ width: '80%' }} onClick={() => handleAssign(syllabus)}>Gán bài tập</Button>
                                    <img src='https://cdn-icons-png.freepik.com/256/4295/4295914.png?semt=ais_hybrid'
                                        style={{ width: "60%", objectFit: "cover", objectPosition: "center" }}
                                    />
                                </Stack>
                            </Stack>
                        </TabPanel>
                    ))}
                </TabContext>
                : <Box sx={{ textAlign: "center" }}>
                    <img src={emptyBook} style={{ height: "200px" }} />
                    <Typography>Hiện không có giáo trình nào!</Typography>
                </Box>}
            <DeleteConfirmationModal
                open={openDeleteConfirm}
                handleClose={() => setOpenDeleteConfirm(false)}
                handleDelete={handleDelete}
            />
            <LoadingComponent open={loading} setOpen={setLoading} />

        </Stack>
    )
}