import { TabContext, TabPanel } from '@mui/lab';
import { Box, Button, Typography, IconButton, Tabs, Tab } from '@mui/material';
import React, { useEffect, useState } from 'react';
import ElevatorIcon from '@mui/icons-material/Elevator';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import ListAltIcon from '@mui/icons-material/ListAlt';
import CurriculumEditedTable from './EditedTable/CurriculumEditedTable';
import CreateOrEditModal from './CurriculumModal/CreateOrEditModal';
import DeleteConfirmationModal from './CurriculumModal/DeleteConfirmationModal';
import emptyBook from '~/assets/images/icon/emptybook.gif'
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import { useSelector } from 'react-redux';
import { tutorInfor } from '~/redux/features/tutorSlice';

function CurriculumManagement() {
    const [valueCurriculum, setValueCurriculum] = useState('1');
    const [curriculumData, setCurriculumData] = useState([]);
    const [openCreateEdit, setOpenCreateEdit] = useState(false);
    const [openDeleteConfirm, setOpenDeleteConfirm] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [currentEditIndex, setCurrentEditIndex] = useState(null);
    const [idDelete, setIdDelete] = useState(0);
    const [index, setIndex] = useState(0);
    const [showTable, setShowTable] = useState(false);
    const tutorInfo = useSelector(tutorInfor);

    useEffect(() => {
        handleGetCurriculums();
    }, [showTable]);

    const handleGetCurriculums = async () => {
        try {
            await services.CurriculumManagementAPI.getCurriculums((res) => {
                if (res?.result) {
                    setCurriculumData(res.result?.sort((a, b) => a.ageFrom - b.ageFrom));
                }
            }, (error) => {
                console.log(error);
            }, {
                status: 'approve',
                orderBy: 'createdDate',
                sort: 'asc',
                pageNumber: 1
            });
        } catch (error) {
            console.log(error);
        }
    };


    const handleChangeCurriculum = (event, newValue) => {
        setValueCurriculum(newValue);
    };

    const handleOpenCreate = () => {
        setOpenCreateEdit(true);
        setIsEditing(false);
        setCurrentEditIndex(null);
    };

    const handleOpenEdit = (index) => {
        setIsEditing(true);
        setCurrentEditIndex(index);
        setOpenCreateEdit(true);
    };

    const handleSubmitCreate = async (formData) => {
        try {
            await services.CurriculumManagementAPI.createCurriculum({
                ageFrom: formData.ageFrom,
                ageEnd: formData.ageEnd,
                description: formData.description,
                originalCurriculumId: 0
            }, (res) => {
                console.log(formData);
                enqueueSnackbar("Tạo khung chương trình thành công!", { variant: "success" });
                setOpenCreateEdit(false);
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }

    };


    const handleSubmitEdit = async (formData, originalCurriculumId) => {
        try {
            await services.CurriculumManagementAPI.createCurriculum({
                ageFrom: formData.ageFrom,
                ageEnd: formData.ageEnd,
                description: formData.description,
                originalCurriculumId
            }, (res) => {
                enqueueSnackbar("Cập nhật khung chương trình thành công!", { variant: "success" });
                setOpenCreateEdit(false);
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }

    };

    const handleOpenDeleteConfirm = (id, index) => {
        setIdDelete(id);
        setIndex(index);
        setOpenDeleteConfirm(true);
    };

    const handleDelete = async () => {
        if (!idDelete) {
            enqueueSnackbar("Xoá khung chương trình thất bại!", { variant: 'error' });
            setOpenDeleteConfirm(false);
            return;
        }
        try {
            await services.CurriculumManagementAPI.deleteCurriculum(idDelete, {}, (res) => {
                const updatedCurriculums = curriculumData.filter((c) => c.id !== idDelete);
                setCurriculumData(updatedCurriculums);
                setOpenDeleteConfirm(false);
                enqueueSnackbar("Xoá khung chương trình thành công!", { variant: 'success' });
                if (valueCurriculum === (index + 1).toString()) {
                    setValueCurriculum('1');
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        }

    };

    const handleShowTable = () => {
        setShowTable(!showTable);
    };

    return (
        <Box sx={{ width: "90%", margin: "auto", mt: "20px", gap: 2 }}>

            {showTable ? (
                <CurriculumEditedTable setShowTable={setShowTable} />
            ) : (
                <>
                    <Typography mb={4} variant='h4'>Khung chương trình học</Typography>
                    <Box sx={{ display: 'flex', justifyContent: 'flex-end' }} gap={2}>
                        <Button color='primary' variant='contained' startIcon={<ListAltIcon />} onClick={handleShowTable}>
                            Danh Sách Đã Sửa
                        </Button>
                        <Button color='primary' variant='contained' startIcon={<AddIcon />} onClick={handleOpenCreate}>
                            Tạo khung chương trình
                        </Button>
                    </Box>
                    {curriculumData.length === 0 ? <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBook} style={{ height: "200px" }} />
                            <Typography>Hiện tại không có khung chương trình!</Typography>
                        </Box> :
                        <TabContext value={valueCurriculum}>
                            <Box sx={{ maxWidth: { xs: 320, sm: 750 } }}>
                                <Tabs
                                    value={valueCurriculum}
                                    onChange={handleChangeCurriculum}
                                    aria-label="icon position tabs example"
                                    scrollButtons
                                    variant="scrollable"
                                >
                                    {curriculumData?.map((curriculum, index) => (
                                        <Tab
                                            key={index}
                                            value={(index + 1).toString()}
                                            icon={<ElevatorIcon />}
                                            iconPosition="start"
                                            label={(
                                                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                                                    Từ {curriculum.ageFrom} - {curriculum.ageEnd} tuổi
                                                    <IconButton
                                                        size="small"
                                                        onClick={() => {
                                                            handleOpenEdit(curriculum);
                                                        }}
                                                        color="default"
                                                        sx={{ ml: 1 }}
                                                    >
                                                        <EditIcon />
                                                    </IconButton>
                                                    <IconButton
                                                        size="small"
                                                        onClick={() => {
                                                            handleOpenDeleteConfirm(curriculum?.id, index);
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
                            {curriculumData.map((curriculum, index) => (
                                <TabPanel key={index} value={(index + 1).toString()}>
                                    <Box ml={2} dangerouslySetInnerHTML={{ __html: curriculum.description }}></Box>
                                </TabPanel>
                            ))}
                        </TabContext>
                    }

                </>
            )}

            {openCreateEdit && tutorInfo && <CreateOrEditModal
                open={openCreateEdit}
                handleClose={() => { setOpenCreateEdit(false); setCurrentEditIndex(null); }}
                handleSubmit={isEditing ? handleSubmitEdit : handleSubmitCreate}
                initialData={isEditing ? currentEditIndex : null}
                isEditing={isEditing}
                tutorProfile={tutorInfo?.tutorProfile}
            />}

            {openDeleteConfirm && <DeleteConfirmationModal
                open={openDeleteConfirm}
                handleClose={() => setOpenDeleteConfirm(false)}
                handleDelete={handleDelete}
            />}
        </Box>
    );
}

export default CurriculumManagement;
